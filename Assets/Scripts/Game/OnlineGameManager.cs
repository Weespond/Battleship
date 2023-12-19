using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Mirror;
using TMPro;

public class OnlineGameManager : NetworkBehaviour
{
    public enum Side
    {
        Left,
        Right,
        Default
    }

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

    [Header("Players")]
    [SerializeField] private GameObject ownPlayer = null;
    [SerializeField] private GameObject opponentPlayer = null;
    [SerializeField] private GameObject ownGamePlayer = null;
    [SerializeField] private GameObject opponentGamePlayer = null;

    [Header("Terrain")]
    [SerializeField] private GameObject ownTerrain = null;
    [SerializeField] private GameObject opponentTerrain = null;

    [Header("Grids")]
    [SerializeField] private GameObject ownGrid = null;
    [SerializeField] private GameObject opponentGrid = null;

    [Header("UI")]
    [SerializeField] private GameObject ownCanvas = null;
    [SerializeField] private GameObject opponentCanvas = null;
    [SerializeField] private GameObject sharedCanvas = null;

    [SerializeField] private GameObject overlayHitMarkers = null;
    [SerializeField] private GameObject overlayEndGame = null;    

    [SerializeField] private TMP_Text ownNameInfo = null;
    [SerializeField] private TMP_Text opponentNameInfo = null;
    [SerializeField] private TMP_Text whoseTurnInfo = null;
    [SerializeField] private Animator loadingRingAnimator = null;

    [Header("Game")]
    [SerializeField] private GameObject battlefield = null;
    [SyncVar] private Side whoseTurn = Side.Default;
    [SyncVar] private string placement_01 = "";
    [SyncVar] private string placement_02 = "";

    [Header("Stats")]
    [SyncVar] private int shotCount_01 = 0;
    [SyncVar] private int shotCount_02 = 0;

    [SyncVar] private int hitCount_01 = 0;
    [SyncVar] private int hitCount_02 = 0;


    public Side WhoseTurn { get { return whoseTurn; } set { whoseTurn = value; } }
    public string Placement_01 { get { return placement_01; } set { placement_01 = value; } }
    public string Placement_02 { get { return placement_02; } set { placement_02 = value; } }
    public Side OwnGamePlayerSide() { return ownGamePlayer.GetComponent<NetworkGamePlayer>().mySide; }

    public override void OnStartClient()
    {
        sharedCanvas = GameObject.Find("SharedCanvas");
        sharedCanvas.transform.Find("NameTags").gameObject.SetActive(false);

        overlayHitMarkers = GameObject.Find("MainCamera").transform.Find("Canvas").transform.Find("OverlayHitMarkers").gameObject;
        overlayEndGame = GameObject.Find("MainCamera").transform.Find("Canvas").transform.Find("OverlayEndGame").gameObject;
        whoseTurnInfo = GameObject.Find("MainCamera").transform.Find("Canvas").transform.Find("WhoseTurnInfo").gameObject.GetComponent<TMP_Text>();

        GameObject.Find("MainCamera").transform.Find("Canvas").transform.Find("Bar").transform.Find("Panel").gameObject.SetActive(false);

        if (isServer && isClient) // meaning we are host's OGM
        {
            ownGrid = GameObject.Find("Grid_01");
            opponentGrid = GameObject.Find("Grid_02");

            ownCanvas = GameObject.Find("Canvas_01");
            opponentCanvas = GameObject.Find("Canvas_02");

            ownTerrain = GameObject.Find("Terrain_01");
            opponentTerrain = GameObject.Find("Terrain_02");

            sharedCanvas.transform.Find("Coords").Find("Right").gameObject.SetActive(false);
            sharedCanvas.transform.Find("LoadingRings").Find("Left").gameObject.SetActive(false);

            battlefield = sharedCanvas.transform.Find("Battlefields").Find("Field (right)").gameObject;

            ownNameInfo = sharedCanvas.transform.Find("NameTags").Find("Name_01").gameObject.GetComponent<TMP_Text>();
            opponentNameInfo = sharedCanvas.transform.Find("NameTags").Find("Name_02").gameObject.GetComponent<TMP_Text>();
        }
        else if (isClientOnly) // meaning we are clientOnly's OGM
        {
            ownGrid = GameObject.Find("Grid_02");
            opponentGrid = GameObject.Find("Grid_01");

            ownCanvas = GameObject.Find("Canvas_02");
            opponentCanvas = GameObject.Find("Canvas_01");

            ownTerrain = GameObject.Find("Terrain_02");
            opponentTerrain = GameObject.Find("Terrain_01");

            sharedCanvas.transform.Find("Coords").Find("Left").gameObject.SetActive(false);
            sharedCanvas.transform.Find("LoadingRings").Find("Right").gameObject.SetActive(false);

            battlefield = sharedCanvas.transform.Find("Battlefields").Find("Field (left)").gameObject;

            ownNameInfo = sharedCanvas.transform.Find("NameTags").Find("Name_02").gameObject.GetComponent<TMP_Text>();
            opponentNameInfo = sharedCanvas.transform.Find("NameTags").Find("Name_01").gameObject.GetComponent<TMP_Text>();
        }
        /////////

        GameObject[] players = new GameObject[2];
        players = GameObject.FindGameObjectsWithTag("GamePlayer");

        foreach (GameObject player in players)
        {
            if (player.GetComponent<NetworkIdentity>().hasAuthority)
            {
                ownGamePlayer = player;
            }
            else
            {
                opponentGamePlayer = player;
            }
        }

        // destroy opponent's ships on this client (it is possible, that the grid itself wont be neccessary)
        foreach (Transform child in opponentGrid.transform)
        {
            Destroy(child.gameObject);
        }

        // disable opponent's buttons and fleet terrain
        opponentCanvas.transform.Find("Panel").gameObject.SetActive(false);
        opponentTerrain.gameObject.SetActive(false);

    }

    [ClientRpc]
    public void RpcUpdateLoadingRings()
    {
        NetworkGamePlayer gamePlayer = opponentGamePlayer.GetComponent<NetworkGamePlayer>();

        if (gamePlayer.mySide == Side.Left) // meaning, that our opponent is in the left
        {
            sharedCanvas.transform.Find("LoadingRings").Find("Left").gameObject.SetActive(!gamePlayer.placementIsReady);
        }
        else if (gamePlayer.mySide == Side.Right) // meaning, that our opponent is in the right
        {
            sharedCanvas.transform.Find("LoadingRings").Find("Right").gameObject.SetActive(!gamePlayer.placementIsReady);
        }
    }

    [ClientRpc]
    public void RpcUpdateBattleFieldsAfterPlacement()
    {
        if (whoseTurn == ownGamePlayer.GetComponent<NetworkGamePlayer>().mySide)
        {
            battlefield.SetActive(opponentGamePlayer.GetComponent<NetworkGamePlayer>().placementIsReady);

            whoseTurnInfo.gameObject.SetActive(true);
            whoseTurnInfo.text = "<color #ffffff>Ваш ход</color>";

        }
        else
        {
            whoseTurnInfo.gameObject.SetActive(ownGamePlayer.GetComponent<NetworkGamePlayer>().placementIsReady);
            whoseTurnInfo.text = "<color #999999>Ход соперника</color>";
        }
    }

    [ClientRpc]
    public void RpcUpdateBattleFields()
    {
        if (whoseTurn == ownGamePlayer.GetComponent<NetworkGamePlayer>().mySide)
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
    public void GoBackToMainMenu() => Room.IngameDisconnect();
    public void ReadyToBattle()
    {
        sharedCanvas.transform.Find("Coords").Find("Left").gameObject.SetActive(true);
        sharedCanvas.transform.Find("Coords").Find("Right").gameObject.SetActive(true);

        GameObject.Find("MainCamera").transform.Find("Canvas").Find("Bar").Find("Panel").gameObject.SetActive(true); // activate bar

        ownCanvas.transform.Find("Panel").gameObject.SetActive(false);

        opponentTerrain.gameObject.SetActive(true);

        ownGamePlayer.GetComponent<NetworkGamePlayer>().CmdReadyUp();

        sharedCanvas.transform.Find("NameTags").gameObject.SetActive(true);
        ownNameInfo.text = "<color #ff00ff>" + ownGamePlayer.GetComponent<NetworkGamePlayer>().displayName + "</color>";
        opponentNameInfo.text = "<color #ff0000>" + opponentGamePlayer.GetComponent<NetworkGamePlayer>().displayName + "</color>";

        string myplacement = ownGamePlayer.GetComponent<NetworkGamePlayer>().GetMyPlacement();
        CmdSetPlacement(ownGamePlayer.GetComponent<NetworkGamePlayer>().mySide, myplacement);

        GameObject[] players = new GameObject[2];
        players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            if (player.GetComponent<NetworkIdentity>().hasAuthority)
            {
                ownPlayer = player;
            }
            else
            {
                opponentPlayer = player;
            }
        }

        StartCoroutine(PlayerLerpToCenter(ownPlayer));
    }

    private IEnumerator PlayerLerpToCenter(GameObject player)
    {
        Vector3 startPos = player.transform.localPosition;
        Vector3 endPos = new Vector3(500f, 120f, 110f);
        float time = .5f;
        float elapsedTime = 0;

        while (elapsedTime < time)
        {
            ownPlayer.transform.position = Vector3.Lerp(startPos, endPos, (elapsedTime / time));
            ownPlayer.transform.GetChild(0).GetComponent<CinemachineVirtualCamera>().m_Lens.FieldOfView = Mathf.Lerp(60, 75, elapsedTime / time);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetPlacement(Side side, string placement)
    {
        if (side == Side.Left)
        {
            Placement_01 = placement;
        }
        else if (side == Side.Right)
        {
            Placement_02 = placement;
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdShootAndUpdateCell(Side shooter, int row, int column) // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! //
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
        targetCells[row, column] = hit ? 4 : 3;

        Room.SpawnMarkerHitOrMiss(shooter, row, column, hit);

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
                        Room.SpawnMarkerHitOrMiss(shooter, i - 1, j - 1, false);
                    }

                    if (i >= 1 && targetCells[i - 1, j] == 0)
                    {
                        targetCells[i - 1, j] = 3; // top
                        Room.SpawnMarkerHitOrMiss(shooter, i - 1, j, false);
                    }

                    if (i >= 1 && j <= 8 && targetCells[i - 1, j + 1] == 0)
                    {
                        targetCells[i - 1, j + 1] = 3; // top right     
                        Room.SpawnMarkerHitOrMiss(shooter, i - 1, j + 1, false);
                    }

                    if (j <= 8 && targetCells[i, j + 1] == 0)
                    {
                        targetCells[i, j + 1] = 3; // right
                        Room.SpawnMarkerHitOrMiss(shooter, i, j + 1, false);
                    }

                    if (i <= 8 && j <= 8 && targetCells[i + 1, j + 1] == 0)
                    {
                        targetCells[i + 1, j + 1] = 3; // bot right
                        Room.SpawnMarkerHitOrMiss(shooter, i + 1, j + 1, false);
                    }

                    if (i <= 8 && targetCells[i + 1, j] == 0)
                    {
                        targetCells[i + 1, j] = 3; // bot
                        Room.SpawnMarkerHitOrMiss(shooter, i + 1, j, false);
                    }

                    if (i <= 8 && j >= 1 && targetCells[i + 1, j - 1] == 0)
                    {
                        targetCells[i + 1, j - 1] = 3; // bot left
                        Room.SpawnMarkerHitOrMiss(shooter, i + 1, j - 1, false);
                    }

                    if (j >= 1 && targetCells[i, j - 1] == 0)
                    {
                        targetCells[i, j - 1] = 3; // left
                        Room.SpawnMarkerHitOrMiss(shooter, i, j - 1, false);
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

                }
            }
        }

        // for stat
        if (shooter == Side.Left)
        {
            shotCount_01++;
            if (hit)
            {
                hitCount_01++;
            }
        }
        else if (shooter == Side.Right)
        {
            shotCount_02++;
            if (hit)
            {
                hitCount_02++;
            }
        }

        targetPlacement = "";
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

        if (hitCount_01 == 20)
        {
            StartCoroutine(AnnounceWinnerAfterSeconds(Side.Left, 1f));
        }
        else if (hitCount_02 == 20)
        {
            StartCoroutine(AnnounceWinnerAfterSeconds(Side.Right, 1f));
        }
    }
    [ClientRpc]
    public void RpcDisableBattlefield() => battlefield.SetActive(false);

    [Server]
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
    public void ShootCell(Button contextButton)
    {
        if (WhoseTurn != ownGamePlayer.GetComponent<NetworkGamePlayer>().mySide)
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


        CmdTriggerOverlayAnimator(hit);
        CmdShootAndUpdateCell(WhoseTurn, row, column);

        if (!hit)
        {
            CmdSwitchTurn();
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdTriggerOverlayAnimator(bool hit) => RpcTriggerMyOverlayAnimator(hit);


    [ClientRpc]
    public void RpcTriggerMyOverlayAnimator(bool hit)
    {
        Animator animator = overlayHitMarkers.GetComponent<Animator>();
        if (!hit)
        {
            animator.SetTrigger("Miss");
        }
        else
        {
            if (ownGamePlayer.GetComponent<NetworkGamePlayer>().mySide == WhoseTurn)
            {
                animator.SetTrigger("Hit");
            }
            else
            {
                animator.SetTrigger("GotHit");
            }
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdSwitchTurn()
    {
        WhoseTurn = WhoseTurn == Side.Left ? Side.Right : Side.Left;
        StartCoroutine(UpdateFieldsAfterSeconds(.5f));
    }

    private IEnumerator UpdateFieldsAfterSeconds(float count)
    {
        yield return new WaitForSeconds(count);
        RpcUpdateBattleFields();
    }

    [ClientRpc]
    public void RpcAnnounceWinner(Side winner)
    {
        overlayEndGame.SetActive(true);
        overlayEndGame.GetComponent<Animator>().SetTrigger("GameEnded");

        NetworkGamePlayer ownGP = ownGamePlayer.GetComponent<NetworkGamePlayer>();
        NetworkGamePlayer oppGP = opponentGamePlayer.GetComponent<NetworkGamePlayer>();

        TMP_Text winloose = overlayEndGame.transform.Find("Panel").transform.Find("WinLoose").GetComponent<TMP_Text>();
        Image winlooseBackground = overlayEndGame.transform.Find("Panel").transform.Find("WinLooseBackground").GetComponent<Image>();
        TMP_Text name_01 = overlayEndGame.transform.Find("Panel").transform.Find("Stats").Find("Name_01").GetComponent<TMP_Text>();
        TMP_Text name_02 = overlayEndGame.transform.Find("Panel").transform.Find("Stats").Find("Name_02").GetComponent<TMP_Text>();
        TMP_Text acc_01 = overlayEndGame.transform.Find("Panel").transform.Find("Stats").Find("Acc_01").GetComponent<TMP_Text>();
        TMP_Text acc_02 = overlayEndGame.transform.Find("Panel").transform.Find("Stats").Find("Acc_02").GetComponent<TMP_Text>();

        float accuracy_01 = shotCount_01 > 0 ? (float)hitCount_01 * 100 / (float)shotCount_01 : 0;
        float accuracy_02 = shotCount_02 > 0 ? (float)hitCount_02 * 100 / (float)shotCount_02 : 0;

        if (ownGP.mySide == winner)
        {
            winloose.text = "<color #ffffff>Победа</color>";
            winlooseBackground.color = new Color(0f, 160f/255f, 50f/255f, 30f/255f);
        }
        else
        {
            winloose.text = "<color #ffffff>Поражение</color>";
            winlooseBackground.color = new Color(160f/255f, 10f/255f, 0f, 30f/255f);
        }

        acc_01.text = accuracy_01.ToString("0.000") + "%";
        acc_02.text = accuracy_02.ToString("0.000") + "%";

        if (ownGP.mySide == Side.Left)
        {
            name_01.text = ownGP.displayName;
            name_02.text = oppGP.displayName;
            
        }
        else if (ownGP.mySide == Side.Right)
        {
            name_01.text = oppGP.displayName;
            name_02.text = ownGP.displayName;
        }
    }

    [ClientRpc]
    public void RpcSetLoadingRingActive(bool value) => loadingRingAnimator.SetBool("Loading", value);

    private IEnumerator AnnounceWinnerAfterSeconds(Side winner, float count)
    {
        RpcDisableBattlefield();
        RpcSetLoadingRingActive(true);

        yield return new WaitForSeconds(count);

        /////////////////////////////////////////////////////////////        

        foreach (var gamePlayer in Room.GamePlayers)
        {
            int winCount = 0;
            int matchCount = 0;
            int hitCount = 0;
            int shotCount = 0;

            bool doneHere = false;

            while (!doneHere)
            {
                var get = new UnityWebRequest(ScoreManager.webURL + ScoreManager.publicCode + "/pipe-get/" + gamePlayer.displayName);
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
                }
                else
                {
                    print("get error: " + get.error);
                    yield return new WaitForSeconds(1f);
                }
            }

            winCount = winner == gamePlayer.mySide ? winCount + 1 : winCount;
            matchCount++;

            hitCount = gamePlayer.mySide == Side.Left ? hitCount + hitCount_01 : hitCount + hitCount_02;
            shotCount = gamePlayer.mySide == Side.Left ? shotCount + shotCount_01 : shotCount + shotCount_02;

            string newPlayerInfo = string.Format("{0}/{1}/{2}/{3}|{4}", gamePlayer.displayName, winCount, matchCount, hitCount, shotCount);

            doneHere = false;

            while (!doneHere)
            {
                var post = new UnityWebRequest(ScoreManager.webURL + ScoreManager.privateCode + "/add/" + newPlayerInfo);
                yield return post.SendWebRequest();

                if (string.IsNullOrEmpty(post.error))
                {
                    print("post succes! " + newPlayerInfo);
                    doneHere = true;
                }
                else
                {
                    print("post error: " + post.error);
                    yield return new WaitForSeconds(1f);
                }
            }
        }
        RpcSetLoadingRingActive(false);
        RpcAnnounceWinner(winner);
    }
}
