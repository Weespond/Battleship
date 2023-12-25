using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Mirror;
using TMPro;

public class GameManager : MonoBehaviour
{
    public enum Side
    {
        Left,
        Right,
        Default
    }
    public enum Tactic
    {
        Random,
        Diagonal,
        FastCarrier
    }

    [Header("Players")]
    [SerializeField] private GameObject ownPlayer = null;

    [Header("Terrain")]
    [SerializeField] private GameObject opponentTerrain = null;

    [Header("Grids")]
    [SerializeField] private GameObject ownGrid = null;
    [SerializeField] private GameObject opponentGrid = null;

    [Header("UI")]
    [SerializeField] private GameObject ownCanvas = null;
    [SerializeField] private GameObject sharedCanvas = null;
    [SerializeField] private GameObject overlayHitMarkers = null;
    [SerializeField] private GameObject overlayEndGame = null;
    [SerializeField] private TMP_Text ownNameInfo = null;
    [SerializeField] public  TMP_Text opponentNameInfo = null;
    [SerializeField] private TMP_Text whoseTurnInfo = null;
    [SerializeField] private Animator loadingRingAnimator = null;

    [Header("Game")]
    [SerializeField] private GameObject battlefield = null;
    [SerializeField] private GameObject hitDeckPrefab = null;
    [SerializeField] private GameObject missSplashPrefab = null;
    [SerializeField] private GameObject justDeckPrefab = null;

    [Header("AI stuff")]
    private Tactic aiTactic = Tactic.Random;

    public Side WhoseTurn { get; set; } = Side.Default;
    public string Placement_01 { get; set; } = "";
    public string Placement_02 { get; set; } = "";

    public int ShotCount_01 { get; set; } = 0;
    public int ShotCount_02 { get; set; } = 0;
    public int HitCount_01 { get; set; } = 0;
    public int HitCount_02 { get; set; } = 0;

    public int FirstHitIndex { get; set; } = -1;
    public int LastHitIndex { get; set; } = -1;    
    public int GetTactic() { return (int)aiTactic; }
    public void SetTactic(int val) { aiTactic = (Tactic)val; }

    public bool ShipThatWasHitIsHorizontal { get; set; } = false;
    public bool ShipThatWasHitIsVertical { get; set; } = false;

    private NetworkManagerBS room;
    private NetworkManagerBS Room
    {
        get
        {
            if (room != null)
            {
                return room;
            }
            return room = NetworkManager.singleton as NetworkManagerBS;
        }
    }

    public void GoBackToMainMenu() => Room.IngameDisconnect();
    public void Start() // AI difficulty => ship placement + tactic
    {
        ShipsGrid aiGrid = opponentGrid.GetComponent<ShipsGrid>();
        int difficulty = Difficulty.difficultyValue;
        int rnd = Random.Range(0, 3);

        if (difficulty == 0) //Легкий
        {
            aiTactic = Tactic.Random;
            aiGrid.AutoPlacement_Random();
        }
        else if (difficulty == 1) //Средний
        {
            aiTactic = Tactic.Random;
            aiGrid.AutoPlacement_Diagonal();
        }
        else if (difficulty == 2) //Тяжелый
        {
            aiTactic = Tactic.Diagonal;
            aiGrid.AutoPlacement_Coasts();
        }
        else  //Мистический
        {
            aiTactic = (Tactic)rnd;

            if (rnd == 0)
            {
                aiGrid.AutoPlacement_Random();
            }
            else if (rnd == 1)
            {
                aiGrid.AutoPlacement_Diagonal();
            }
            else
            {
                aiGrid.AutoPlacement_Coasts();
            }
        }
                        
        opponentTerrain.gameObject.SetActive(false);

        foreach (Transform child in opponentGrid.transform)
        {
            child.gameObject.SetActive(false);
        }
    }
    public void UpdateBattleFields()
    {
        if (WhoseTurn == Side.Left)
        {
            battlefield.SetActive(true);
            whoseTurnInfo.text = "<color #ffffff>Ваш ход</color>";
        }
        else
        {
            battlefield.SetActive(false);
            whoseTurnInfo.text = "<color #999999>Ход соперника</color>";
        }
    }
    public void ReadyToBattle()
    {
        sharedCanvas.transform.Find("Coords").Find("Left").gameObject.SetActive(true);
        sharedCanvas.transform.Find("Coords").Find("Right").gameObject.SetActive(true);

        GameObject barPanel = GameObject.Find("MainCamera").transform.Find("Canvas").Find("Bar").Find("Panel").gameObject; // bar
        barPanel.SetActive(true);

        ownCanvas.transform.Find("Panel").gameObject.SetActive(false);

        opponentTerrain.gameObject.SetActive(true);

        sharedCanvas.transform.Find("NameTags").gameObject.SetActive(true);
        ownNameInfo.text = "<color #ff00ff>" + Authorization.nickname + "</color>";
        opponentNameInfo.text = "<color #ff0000>Противник (" + (Difficulty.difficultyValue == 3 ? "мистический" : Difficulty.difficultyValue == 2 ? "тяжело" : Difficulty.difficultyValue == 1 ? "средне" : "легко") + ")</color>";

        string myplacement = "";
        int[,] cells = ownGrid.GetComponent<ShipsGrid>().GridCells;
 
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                myplacement += cells[i, j];
            }
        }
        Placement_01 = myplacement;

        foreach (Transform child in opponentGrid.transform)
        {
            child.gameObject.SetActive(true);
        }

        string opponentPlacement = "";

        opponentGrid.GetComponent<ShipsGrid>().EndPlacement();

        cells = opponentGrid.GetComponent<ShipsGrid>().GridCells;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                opponentPlacement += cells[i, j];
            }
        }
        Placement_02 = opponentPlacement;

        foreach (Transform child in opponentGrid.transform)
        {
            child.gameObject.SetActive(false);
        }
        //// set placements

        WhoseTurn = Side.Left;
        whoseTurnInfo.gameObject.SetActive(true);
        whoseTurnInfo.text = "<color #ffffff>Ваш ход</color>";

        battlefield.SetActive(true);

        StartCoroutine(PlayerLerpToCenter());
    }
    private IEnumerator PlayerLerpToCenter() // move camera, change its fov
    {
        Vector3 startPos = ownPlayer.transform.localPosition;
        Vector3 endPos = new Vector3(500f, 120f, 110f);
        float time = .5f;
        float elapsedTime = 0;

        while (elapsedTime < time)
        {
            ownPlayer.transform.position = Vector3.Lerp(startPos, endPos, (elapsedTime / time));
            ownPlayer.GetComponent<Camera>().fieldOfView = Mathf.Lerp(60, 75, elapsedTime / time);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    public void ShootCell(Button contextButton) // player shoot call from cell's button on canvas
    {
        if (WhoseTurn != Side.Left)
        {
            return;
        }

        int row = int.Parse(contextButton.gameObject.transform.parent.gameObject.name.Substring(5, 1));
        int column = int.Parse(contextButton.gameObject.name.Substring(6, 1));
        string targetPlacement = WhoseTurn == Side.Left ? Placement_02 : Placement_01;

        if (targetPlacement[10 * row + column] == '3' || targetPlacement[10 * row + column] == '4')
        {
            return;
        }

        bool hit = targetPlacement[10 * row + column] == '1';

        ShootAndUpdateCell(WhoseTurn, row, column);

        if (!hit)
        {
            SwitchTurn();
            UpdateBattleFields();
        }
    }
    public void SwitchTurn() // End turn, start AI turn coroutine if WhoseTurn got changed to Side.Right
    {
        WhoseTurn = WhoseTurn == Side.Left ? Side.Right : Side.Left;
        GameObject barPanel = GameObject.Find("MainCamera").transform.Find("Canvas").Find("Bar").Find("Panel").gameObject;
        if (WhoseTurn == Side.Right)
        {
            StartCoroutine(AITurnCoroutine(1.45f));            
        }
    }


    public IEnumerator AITurnCoroutine(float count) // -------------- AI LOGIC --------------
    {
        bool miss = false;
        while (!miss && HitCount_02 < 20) //проверка на окончание игры
        {
            int index;
            yield return new WaitForSeconds(count);

            if (LastHitIndex >= 0) // if there is a ship that we need to finish
            {
                index = AIChooseCell_FinishShip();
            }
            else
            {
                if (aiTactic == Tactic.Random)
                    index = AIChooseCell_Random();
                else
                    index = AIChooseCell_Diagonal();
            }

            int row = index / 10;
            int column = index % 10;

            if (Placement_01[index] == '0')
            {
                miss = true;   // DEBUG: DONT END AI'S TURN             
            }
            else if (Placement_01[index] == '1')
            {
                if (FirstHitIndex == -1)
                    FirstHitIndex = index;
                LastHitIndex = index;
            }

            ShootAndUpdateCell(Side.Right, row, column);
        }
        if (HitCount_02 != 20)
        {
            yield return new WaitForSeconds(.5f);
            SwitchTurn();
            UpdateBattleFields();
        }

    }
    public int AIChooseCell_FinishShip()
    {
        int index;

        int lastHitRow = LastHitIndex / 10;
        int lastHitColumn = LastHitIndex % 10;

        int[,] targetCells = new int[10, 10];

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                targetCells[i, j] = int.Parse(Placement_01[i * 10 + j].ToString());
            }
        }

        if (FirstHitIndex == LastHitIndex)
        {
            int i = lastHitRow;
            int j = lastHitColumn;

            List<Vector2Int> Directions = new List<Vector2Int>();

            if (i >= 1 && targetCells[i - 1, j] != 3) // can we go up ?
            {
                Directions.Add(new Vector2Int(i - 1, j));
            }
            if (j >= 1 && targetCells[i, j - 1] != 3) // can we go left ?
            {
                Directions.Add(new Vector2Int(i, j - 1));
            }
            if (i <= 8 && targetCells[i + 1, j] != 3) // can we go down ?
            {
                Directions.Add(new Vector2Int(i + 1, j));
            }
            if (j <= 8 && targetCells[i, j + 1] != 3) // can we go right ?
            {
                Directions.Add(new Vector2Int(i, j + 1));
            }

            int d = Random.Range(0, Directions.Count); // choose one direction

            index = Directions[d].x * 10 + Directions[d].y;
        }
        else
        {
            int i = lastHitRow;
            int j = lastHitColumn;

            // what is the ship's orientation?
            ShipThatWasHitIsVertical = (i >= 1 && targetCells[i - 1, j] == 4) || (i <= 8 && targetCells[i + 1, j] == 4);
            ShipThatWasHitIsHorizontal = (j >= 1 && targetCells[i, j - 1] == 4) || (j <= 8 && targetCells[i, j + 1] == 4);

            List<Vector2Int> Directions = new List<Vector2Int>();

            if (ShipThatWasHitIsHorizontal)
            {
                if (j >= 1) //can we go left ?
                {
                    if ((targetCells[i, j - 1] == 0) || (targetCells[i, j - 1] == 1))
                    {
                        Directions.Add(new Vector2Int(i, j - 1));
                    }
                }
                if (j <= 8) // can we go right ?
                {
                    if ((targetCells[i, j + 1] == 0) || (targetCells[i, j + 1] == 1))
                    {
                        Directions.Add(new Vector2Int(i, j + 1));
                    }
                }
            }
            else if (ShipThatWasHitIsVertical)
            {
                if (i >= 1) // can we go up ?
                {
                    if (targetCells[i - 1, j] == 0 || targetCells[i - 1, j] == 1)
                    {
                        Directions.Add(new Vector2Int(i - 1, j));
                    }
                }
                if (i <= 8) // can we go down ?
                {
                    if (targetCells[i + 1, j] == 0 || targetCells[i + 1, j] == 1)
                    {
                        Directions.Add(new Vector2Int(i + 1, j));
                    }
                }
            }

            if (Directions.Count == 0) // if we stuck => go back to firstHitindex
            {
                i = FirstHitIndex / 10;
                j = FirstHitIndex % 10;

                if (ShipThatWasHitIsHorizontal)
                {
                    if (j >= 1 && (targetCells[i, j - 1] == 0) || (targetCells[i, j - 1] == 1)) // can we go left ?
                    {
                        Directions.Add(new Vector2Int(i, j - 1));
                    }
                    if (j <= 8 && (targetCells[i, j + 1] == 0) || (targetCells[i, j + 1] == 1)) // can we go right ?
                    {
                        Directions.Add(new Vector2Int(i, j + 1));
                    }
                }
                else if (ShipThatWasHitIsVertical)
                {
                    if (i >= 1 && (targetCells[i - 1, j] == 0 || targetCells[i - 1, j] == 1)) // can we go up ?
                    {
                        Directions.Add(new Vector2Int(i - 1, j));
                    }
                    if (i <= 8 && (targetCells[i + 1, j] == 0 || targetCells[i + 1, j] == 1)) // can we go down ?
                    {
                        Directions.Add(new Vector2Int(i + 1, j));
                    }
                }
            }

            int d = Random.Range(0, Directions.Count); // choose one direction

            index = Directions[d].x * 10 + Directions[d].y;
        }

        return index;
    }
    public int AIChooseCell_Random()
    {
        List<int> availableCells = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            if (Placement_01[i] == '0' || Placement_01[i] == '1')
            {
                availableCells.Add(i);
            }
        }
        int index = availableCells[Random.Range(0, availableCells.Count)];
        availableCells.Clear();

        return index;
    }
    public int AIChooseCell_Diagonal()
    {
        List<Vector2Int> diagonalCells = new List<Vector2Int>();
        for (int i = 0; i < 10; i++)
        {
            if (Placement_01[i * 10 + i] == '0' || Placement_01[i * 10 + i] == '1')
            {
                diagonalCells.Add(new Vector2Int(i, i));
            }
            if (Placement_01[i * 10 + 9 - i] == '0' || Placement_01[i * 10 + 9 - i] == '1')
            {
                diagonalCells.Add(new Vector2Int(i, 9 - i));
            }
        }

        if (diagonalCells.Count == 0)
        {
            return AIChooseCell_Random();
        }
        else
        {
            int d = Random.Range(0, diagonalCells.Count);
            return diagonalCells[d].x * 10 + diagonalCells[d].y;
        }
    }

    public void ShootAndUpdateCell(Side shooter, int row, int column) // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! //
    {
        string targetPlacement = shooter == Side.Left ? Placement_02 : Placement_01;
        int[,] targetCells = new int[10, 10];

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                targetCells[i, j] = int.Parse(targetPlacement[i * 10 + j].ToString());
            }
        }
        bool hit = targetCells[row, column] == 1;

        overlayHitMarkers.GetComponent<Animator>().SetTrigger(!hit ? "Miss" : shooter == Side.Left ? "Hit" : "GotHit");

        targetCells[row, column] = hit ? 4 : 3;

        SpawnMarkerHitOrMiss(shooter, row, column, hit);

        if (hit)
        {
            if (ThatWasTheLastDeck(targetCells.Clone() as int[,], row, column)) // if that was the last deck
            {
                int i = row;
                int j = column;

                // check if ship was placed horizontally or vertically                 
                bool shipWasVertical = (i >= 1 && targetCells[i - 1, j] == 4) || (i <= 8 && targetCells[i + 1, j] == 4);
                bool shipWasHorizontal = (j >= 1 && targetCells[i, j - 1] == 4) || (j <= 8 && targetCells[i, j + 1] == 4);


                // find first from the top/left deck                
                if (shipWasVertical)
                {
                    while (i > 1 && targetCells[i - 1, j] == 4) { i--; }
                }
                else if (shipWasHorizontal)
                {
                    while (j > 1 && targetCells[i, j - 1] == 4) { j--; }
                }

                bool areWeDoneYet = false;

                // repeat for all decks:
                while (!areWeDoneYet)
                {
                    if (i >= 1 && j >= 1 && targetCells[i - 1, j - 1] == 0)
                    {
                        targetCells[i - 1, j - 1] = 3; // top left
                        SpawnMarkerHitOrMiss(shooter, i - 1, j - 1, false);
                    }

                    if (i >= 1 && targetCells[i - 1, j] == 0)
                    {
                        targetCells[i - 1, j] = 3; // top
                        SpawnMarkerHitOrMiss(shooter, i - 1, j, false);
                    }

                    if (i >= 1 && j <= 8 && targetCells[i - 1, j + 1] == 0)
                    {
                        targetCells[i - 1, j + 1] = 3; // top right     
                        SpawnMarkerHitOrMiss(shooter, i - 1, j + 1, false);
                    }

                    if (j <= 8 && targetCells[i, j + 1] == 0)
                    {
                        targetCells[i, j + 1] = 3; // right
                        SpawnMarkerHitOrMiss(shooter, i, j + 1, false);
                    }

                    if (i <= 8 && j <= 8 && targetCells[i + 1, j + 1] == 0)
                    {
                        targetCells[i + 1, j + 1] = 3; // bot right
                        SpawnMarkerHitOrMiss(shooter, i + 1, j + 1, false);
                    }

                    if (i <= 8 && targetCells[i + 1, j] == 0)
                    {
                        targetCells[i + 1, j] = 3; // bot
                        SpawnMarkerHitOrMiss(shooter, i + 1, j, false);
                    }

                    if (i <= 8 && j >= 1 && targetCells[i + 1, j - 1] == 0)
                    {
                        targetCells[i + 1, j - 1] = 3; // bot left
                        SpawnMarkerHitOrMiss(shooter, i + 1, j - 1, false);
                    }

                    if (j >= 1 && targetCells[i, j - 1] == 0)
                    {
                        targetCells[i, j - 1] = 3; // left
                        SpawnMarkerHitOrMiss(shooter, i, j - 1, false);
                    }


                    if (shipWasVertical && i < 8 && targetCells[i + 1, j] == 4)
                    {
                        i++;
                    }
                    else if (shipWasHorizontal && j < 8 && targetCells[i, j + 1] == 4)
                    {
                        j++;
                    }
                    else
                    {
                        areWeDoneYet = true;
                    }

                    // AI CHECK
                    if (shooter == Side.Right)
                    {
                        ShipThatWasHitIsHorizontal = false;
                        ShipThatWasHitIsVertical = false;
                        FirstHitIndex = -1;
                        LastHitIndex = -1;
                    }

                }
            }
        }// update targetCells[]

        // for stat
        if (shooter == Side.Left)
        {
            ShotCount_01++;
            if (hit)
            {
                HitCount_01++;
            }
        }
        else if (shooter == Side.Right)
        {
            ShotCount_02++;
            if (hit)
            {
                HitCount_02++;
            }
        }

        targetPlacement = ""; // set placement string with targetCells elements
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                targetPlacement += targetCells[i, j].ToString();
            }
        }

        if (shooter == Side.Left)
        {
            Placement_02 = targetPlacement;
        }
        else if (shooter == Side.Right)
        {
            Placement_01 = targetPlacement;
        }

        if (HitCount_01 == 20)
        {
            StartCoroutine(AnnounceWinnerAfterSeconds(Side.Left, .5f));
        }
        else if (HitCount_02 == 20)
        {
            StartCoroutine(AnnounceWinnerAfterSeconds(Side.Right, .5f));
        }
    }
    public bool ThatWasTheLastDeck(int[,] targetCells, int row, int column)
    {
        int i = row;
        int j = column;

        bool shipWasVertical = (i >= 1 && targetCells[i - 1, j] == 4) || (i <= 8 && targetCells[i + 1, j] == 4);
        bool shipWasHorizontal = (j >= 1 && targetCells[i, j - 1] == 4) || (j <= 8 && targetCells[i, j + 1] == 4);

        // find first from the top/left deck                
        if (shipWasVertical)
        {
            while (i > 1 && targetCells[i - 1, j] == 4) { i--; }
        }
        else if (shipWasHorizontal)
        {
            while (j > 1 && targetCells[i, j - 1] == 4) { j--; }
        }

        bool areWeDoneYet = false;

        // repeat for all decks:
        while (!areWeDoneYet)
        {
            if (i >= 1 && j >= 1 && targetCells[i - 1, j - 1] == 1)
                return false;

            if (i >= 1 && targetCells[i - 1, j] == 1)
                return false;

            if (i >= 1 && j <= 8 && targetCells[i - 1, j + 1] == 1)
                return false;

            if (j <= 8 && targetCells[i, j + 1] == 1)
                return false;

            if (i <= 8 && j <= 8 && targetCells[i + 1, j + 1] == 1)
                return false;

            if (i <= 8 && targetCells[i + 1, j] == 1)
                return false;

            if (i <= 8 && j >= 1 && targetCells[i + 1, j - 1] == 1)
                return false;

            if (j >= 1 && targetCells[i, j - 1] == 1)
                return false;



            if (shipWasVertical && i < 8 && targetCells[i + 1, j] == 4)
            {
                i++;
            }
            else if (shipWasHorizontal && j < 8 && targetCells[i, j + 1] == 4)
            {
                j++;
            }
            else
            {
                areWeDoneYet = true;
            }
        }

        return true;
    }
    public void SpawnMarkerHitOrMiss(Side shooter, int row, int column, bool hit)
    {
        Transform parent = shooter == Side.Left ? GameObject.Find("Hitmarkers_01").transform : GameObject.Find("Hitmarkers_02").transform;
        Vector3 position = new Vector3((shooter == Side.Left ? 515 : 395) + (column * 10), 0, 145 - (row * 10));
        GameObject marker = hit ? Instantiate(hitDeckPrefab, parent) : Instantiate(missSplashPrefab, parent);
        marker.transform.position = position;
    }

    public void SpawnJustDeck(Side owner, int row, int column)
    {
        Transform parent = owner == Side.Left ? GameObject.Find("Grid_01").transform : GameObject.Find("Grid_02").transform;
        Vector3 position = new Vector3((owner == Side.Right ? 515 : 395) + (column * 10), 0, 145 - (row * 10));
        GameObject deck =Instantiate(justDeckPrefab, parent);
        deck.transform.position = position;
    }
    private IEnumerator AnnounceWinnerAfterSeconds(Side winner, float count)
    {
        battlefield.SetActive(false);
        int winCount = 0;
        int matchCount = 0;
        int hitCount = 0;
        int shotCount = 0;

        bool doneHere = false;
        loadingRingAnimator.SetBool("Loading", true);

        while (!doneHere)
        {
            var get = new UnityWebRequest(ScoreManager.webURL + ScoreManager.publicCode + "/pipe-get/" + Authorization.nickname);
            get.downloadHandler = new DownloadHandlerBuffer();
            yield return get.SendWebRequest(); 

            if (string.IsNullOrEmpty(get.error))
            {
                string[] entries = get.downloadHandler.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (entries.Length != 0)
                {
                    string[] entryInfo = entries[0].Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (entryInfo.Length != 0)
                    {
                        winCount = int.Parse(entryInfo[1]);
                        matchCount = int.Parse(entryInfo[2]);
                        hitCount = int.Parse(entryInfo[3]);
                        shotCount = int.Parse(entryInfo[4]);
                    }
                }
                print("get succes! " + get.downloadHandler.text);
                doneHere = true;
                loadingRingAnimator.SetBool("Loading", false);
            }
            else
            {
                print("get error: " + get.error);
                yield return new WaitForSeconds(1f);
            }            
        }

        winCount = winner == Side.Left ? winCount + 1 : winCount;
        matchCount++;
        hitCount += HitCount_01;
        shotCount += ShotCount_01;

        string newPlayerInfo = string.Format("{0}/{1}/{2}/{3}|{4}", Authorization.nickname, winCount, matchCount, hitCount, shotCount);

        loadingRingAnimator.SetBool("Loading", true);
        doneHere = false;

        while (!doneHere)
        {
            var post = new UnityWebRequest(ScoreManager.webURL + ScoreManager.privateCode + "/add/" + newPlayerInfo);            
            yield return post.SendWebRequest();

            if (string.IsNullOrEmpty(post.error))
            {
                print("post succes! " + newPlayerInfo);
                doneHere = true;
                loadingRingAnimator.SetBool("Loading", false);
            }
            else
            {
                print("post error: " + post.error);
                yield return new WaitForSeconds(1f); 
            }
        }

        AnnounceWinner(winner);
    }
    public void AnnounceWinner(Side winner)
    {
        overlayEndGame.SetActive(true);
        overlayEndGame.GetComponent<Animator>().SetTrigger("GameEnded");

        TMP_Text winloose = overlayEndGame.transform.Find("Panel").transform.Find("WinLoose").GetComponent<TMP_Text>();
        Image winlooseBackground = overlayEndGame.transform.Find("Panel").transform.Find("WinLooseBackground").GetComponent<Image>();
        TMP_Text name_01 = overlayEndGame.transform.Find("Panel").transform.Find("Stats").Find("Name_01").GetComponent<TMP_Text>();
        TMP_Text name_02 = overlayEndGame.transform.Find("Panel").transform.Find("Stats").Find("Name_02").GetComponent<TMP_Text>();
        TMP_Text acc_01 = overlayEndGame.transform.Find("Panel").transform.Find("Stats").Find("Acc_01").GetComponent<TMP_Text>();
        TMP_Text acc_02 = overlayEndGame.transform.Find("Panel").transform.Find("Stats").Find("Acc_02").GetComponent<TMP_Text>();

        float accuracy_01 = ShotCount_01 > 0 ? (float)HitCount_01 * 100 / (float)ShotCount_01 : 0;
        float accuracy_02 = ShotCount_02 > 0 ? (float)HitCount_02 * 100 / (float)ShotCount_02 : 0;

        if (winner == Side.Left)
        {
            winloose.text = "<color #ffffff>Победа</color>";
            winlooseBackground.color = new Color(0f, 160f / 255f, 50f / 255f, 30f / 255f);
        }
        else
        {
            winloose.text = "<color #ffffff>Поражение</color>";
            winlooseBackground.color = new Color(160f / 255f, 10f / 255f, 0f, 30f / 255f);
        }

        acc_01.text = accuracy_01.ToString("0.000") + "%";
        acc_02.text = accuracy_02.ToString("0.000") + "%";

        name_01.text = Authorization.nickname;
        name_02.text = "Противник (" + (Difficulty.difficultyValue == 2 ? "тяжело" : Difficulty.difficultyValue == 1 ? "средне" : "легко") + ")";
        
    }
}
