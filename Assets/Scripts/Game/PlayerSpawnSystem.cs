using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class PlayerSpawnSystem : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab = null;

    private static List<Transform> spawnPoints = null;

    private int nextIndex = 0;

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

    public static void AddSpawnPoint(Transform transform)
    {
        if (spawnPoints == null)
        {
            spawnPoints = new List<Transform>();            
        }

        spawnPoints.Add(transform);
        spawnPoints = spawnPoints.OrderBy(x => x.gameObject.name).ToList();
    }
    public static void RemoveSpawnPoint(Transform transform)
    {
        spawnPoints.Remove(transform);
    }

    public override void OnStartServer() 
    {
        Room.OnServerReadied += SpawnPlayer;
        Room.OnServerWhenClientDisconnectedIngame += RpcDisconnect;
    }

    [ClientRpc]
    private void RpcDisconnect() => Room.IngameDisconnect();


    [ServerCallback]
    private void OnDestroy() 
    { 
        Room.OnServerReadied -= SpawnPlayer;
        Room.OnServerWhenClientDisconnectedIngame -= RpcDisconnect;
    }

    public override void OnStopServer()
    {
        spawnPoints = null;
        Room.OnServerReadied -= SpawnPlayer;
        NetworkServer.Destroy(gameObject);
    }

    [Server]
    public void SpawnPlayer(NetworkConnection conn)
    {
        if (SceneManager.GetActiveScene().name == "Game_PVP")
        {
            Transform spawnPoint = spawnPoints.ElementAtOrDefault(nextIndex);

            GameObject playerInstance = Instantiate(playerPrefab, spawnPoints[nextIndex].position, spawnPoints[nextIndex].rotation);            
            NetworkServer.Spawn(playerInstance, conn);

            nextIndex++;
        }
    }
}
