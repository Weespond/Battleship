using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using Mirror;
using SFB;



public class NetworkManagerBS : NetworkManager
{

    [Header("Room")]
    [SerializeField] private NetworkRoomPlayer roomPlayerPrefab = null;

    [Header("Game")]
    [SerializeField] private NetworkGamePlayer gamePlayerPrefab = null;
    [SerializeField] private GameObject playerSpawnSystem = null;
    [SerializeField] private GameObject shootHitCube = null;
    [SerializeField] private GameObject shootMissSplash = null;


    private int minPlayers = 2;

    // --------------- LOBBY ----------------
    public event Action OnClientConnectedToLobby;
    public event Action OnClientDisconnectedFromLobby;
    public event Action OnClientHostedTheServer;
    //

    // --------------- GAME ----------------
    public event Action OnServerWhenClientDisconnectedIngame;
    public event Action<NetworkConnection> OnServerReadied;

    public List<NetworkRoomPlayer> RoomPlayers { get; } = new List<NetworkRoomPlayer>();
    public List<NetworkGamePlayer> GamePlayers { get; } = new List<NetworkGamePlayer>();

    public override void OnStartServer()                                // Initialization
    {
        spawnPrefabs.Clear();
        spawnPrefabs = Resources.LoadAll<GameObject>("NetworkIdentified").ToList();
    }
    public override void OnStartClient()                                // Initialization
    {
        spawnPrefabs.Clear();
        spawnPrefabs = Resources.LoadAll<GameObject>("NetworkIdentified").ToList();
        foreach (var prefab in spawnPrefabs)
        {
            ClientScene.RegisterPrefab(prefab);
        }
    }


    public override void OnStopServer()                                 // ---------- LOBBY (i guess) -----------
    {
        RoomPlayers.Clear();
    }
    public override void OnServerConnect(NetworkConnection conn)        // -------- Both LOBBY and GAME --------- (wont let connect more than two players)
    {
        if (numPlayers >= maxConnections)
        {
            conn.Disconnect();
            return;
        }
    }
    public override void OnClientConnect(NetworkConnection conn)        // --------------- LOBBY ---------------- (online menu's UI manipulation: buttons, inputfield, etc.)
    {
        base.OnClientConnect(conn);
        if (numPlayers <= 1)
        {
            OnClientHostedTheServer?.Invoke();
        }
        else
        {
            OnClientConnectedToLobby?.Invoke();
        }
    }
    public override void OnClientDisconnect(NetworkConnection conn)     // -------- Both LOBBY and GAME --------- (invoking some functions)
    {
        base.OnClientDisconnect(conn);
        string currentSceneName = SceneManager.GetActiveScene().name;

        switch (currentSceneName)
        {
            case "Menu":
                {
                    OnClientDisconnectedFromLobby?.Invoke();
                    break;
                }
            case "Game_PVP":
                {
                    //OnServerWhenClientDisconnectedIngame?.Invoke();
                    //Destroy(gameObject);
                    break;
                }
        }
    }
    public override void OnServerDisconnect(NetworkConnection conn)     // -------- Both LOBBY and GAME --------- (invoking some functions)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        switch (currentSceneName)
        {
            case "Menu":
                {
                    if (conn.identity != null)
                    {
                        var player = conn.identity.GetComponent<NetworkRoomPlayer>();
                        RoomPlayers.Remove(player);

                        NotifyPlayersOfReadyState();
                    }

                    base.OnServerDisconnect(conn);

                    break;
                }
            case "Game_PVP":
                {
                    OnServerWhenClientDisconnectedIngame?.Invoke(); // RpcClient function invoking (from spawn system)

                    base.OnServerDisconnect(conn);

                    break;
                }
        }

    }
    public void NotifyPlayersOfReadyState()                             // --------------- LOBBY ---------------- 
    {
        foreach (var player in RoomPlayers)
        {
            player.HandleReadyToStart(IsReadyToStart());
        }
    }
    private bool IsReadyToStart()                                       // --------------- LOBBY ---------------- 
    {
        if (numPlayers < minPlayers)
        {
            return false;
        }
        foreach (var player in RoomPlayers)
        {
            if (!player.IsReady)
            {
                return false;
            }
        }
        return true;
    }
    public override void OnServerAddPlayer(NetworkConnection conn)      // --------------- LOBBY ----------------
    {
        bool isLeader = RoomPlayers.Count == 0;

        NetworkRoomPlayer roomPlayerInstance = Instantiate(roomPlayerPrefab);
        roomPlayerInstance.IsLeader = isLeader;
        NetworkServer.AddPlayerForConnection(conn, roomPlayerInstance.gameObject);
    }
    public void StartGame()                                             // --------------- LOBBY ----------------
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            if (!IsReadyToStart()) { return; }

            ServerChangeScene("Game_PVP");
        }
    }

    public override void ServerChangeScene(string newSceneName)         // --------------- LOBBY ----------------
    {
        //yet only from menu to game
        if (SceneManager.GetActiveScene().name == "Menu" && newSceneName == "Game_PVP")
        {
            for (int i = RoomPlayers.Count - 1; i >= 0; i--)
            {
                var conn = RoomPlayers[i].connectionToClient;
                var gameplayerInstance = Instantiate(gamePlayerPrefab);
                gameplayerInstance.SetDisplayName(RoomPlayers[i].DisplayName);

                NetworkServer.Destroy(conn.identity.gameObject);
                NetworkServer.ReplacePlayerForConnection(conn, gameplayerInstance.gameObject, true);
            }
        }

        FindObjectOfType<AudioManager>().Play("SELECT");

        base.ServerChangeScene(newSceneName);
    }

    public override void OnServerSceneChanged(string sceneName)         // --------------- GAME -----------------
    {
        if (sceneName == "Game_PVP")
        {
            GameObject playerSpawnSystemInstance = Instantiate(playerSpawnSystem);
            NetworkServer.Spawn(playerSpawnSystemInstance);
        }
    }

    public override void OnServerReady(NetworkConnection conn)          // --------------- GAME ----------------- (SpawnPlayer function invoking)
    {
        base.OnServerReady(conn);
        OnServerReadied?.Invoke(conn);
    }
    public void IngameDisconnect()                                      // --------------- GAME -----------------
    {
        if (SceneManager.GetActiveScene().name == "Game_PVP")
        {
            if (mode == NetworkManagerMode.Host)
            {
                StopHost();
                Debug.Log("Stopping the host...");
            }
            else if (mode == NetworkManagerMode.ClientOnly)
            {
                StopClient();
                Debug.Log("Disconnecting...");
            }
        }
        SceneManager.LoadScene("Menu");
        Destroy(gameObject);
    }

    public void SpawnDeckHit(Vector3 position)         // --------------- GAME -----------------
    {
        GameObject deckInstance = Instantiate(shootHitCube);
        deckInstance.transform.position = position;
        NetworkServer.Spawn(deckInstance);
    }

    public void SpawnSplashMiss(Vector3 position)         // --------------- GAME -----------------
    {
        GameObject deckInstance = Instantiate(shootMissSplash);
        deckInstance.transform.position = position;
        NetworkServer.Spawn(deckInstance);
    }

    public void SpawnMarkerHitOrMiss(OnlineGameManager.Side shooter, int row, int column, bool hit)         // --------------- GAME -----------------
    {
        Vector3 position = new Vector3((shooter == OnlineGameManager.Side.Left ? 515 : 395) + (column * 10), 0, 145 - (row * 10));
        GameObject marker = hit ? Instantiate(shootHitCube) : Instantiate(shootMissSplash);
        marker.transform.position = position;
        NetworkServer.Spawn(marker);
    }
    
    public IEnumerator DirtyLoadOfflineGameAfterSeconds(float count, string[] path)
    {
        SceneManager.LoadScene("Game_PVE");
        
        yield return new WaitForSeconds(count);
        //start game somehow
        GameObject.Find("GameManager").GetComponent<GameManager>().ReadyToBattle();
    }

    public void DirtyLoadOfflineGame()
    {
        var extensions = new[] {
            new ExtensionFilter("Data", "dat"),
        };
        var path = StandaloneFileBrowser.OpenFilePanel("Open File", "C:\\Users\\user\\Desktop\\", extensions, false);

        if (path.Length > 0)
        {
            StartCoroutine(DirtyLoadOfflineGameAfterSeconds(2f,path));        
        }
    }


}
