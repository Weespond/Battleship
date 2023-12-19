using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class NetworkGamePlayer : NetworkBehaviour
{
    [SyncVar] public string displayName = "Загрузка...";
    [SyncVar] public bool placementIsReady = false;
    [SyncVar] public OnlineGameManager.Side mySide = OnlineGameManager.Side.Default;    

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


    private OnlineGameManager ogm;
    private OnlineGameManager OGM
    {
        get
        {
            if (ogm != null)
            {
                return ogm;
            }
            return ogm = GameObject.Find("GameManager").GetComponent<OnlineGameManager>();
        }
    }

    public override void OnStartClient()
    {
        DontDestroyOnLoad(gameObject);
        Room.GamePlayers.Add(this);
        CmdChooseSide();        
    }

    public string GetMyPlacement()
    {
        string myplacement = "";

        int[,] cells = new int[10, 10];

        if (mySide == OnlineGameManager.Side.Left)
        {
            cells = GameObject.Find("Grid_01").GetComponent<ShipsGrid>().GridCells;
        }
        else if (mySide == OnlineGameManager.Side.Right)
        {
            cells = GameObject.Find("Grid_02").GetComponent<ShipsGrid>().GridCells;
        }

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                myplacement += cells[i, j];
            }
        }
        return myplacement;
    }

    public override void OnStopClient()
    {
        Room.GamePlayers.Remove(this);
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
    }

    [Server]
    public void SetDisplayName(string name) => displayName = name;

    [Command]
    public void CmdReadyUp()
    {
        placementIsReady = true;

        if (OGM.WhoseTurn == OnlineGameManager.Side.Default)
        {
            OGM.WhoseTurn = mySide;
        }        

        StartCoroutine(UpdateStuff(1f));
    }

    [Command(ignoreAuthority = true)]
    public void CmdChooseSide() => mySide = hasAuthority ? OnlineGameManager.Side.Left : mySide = OnlineGameManager.Side.Right;
    private IEnumerator UpdateStuff(float count)
    {
        yield return new WaitForSeconds(count);
        OGM.RpcUpdateLoadingRings();
        OGM.RpcUpdateBattleFieldsAfterPlacement();
    }
}
