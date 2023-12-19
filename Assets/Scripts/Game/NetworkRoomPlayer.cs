using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class NetworkRoomPlayer : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject lobbyUI = null;
    [SerializeField] private TMP_Text[] playerNameTexts = new TMP_Text[2];
    [SerializeField] private TMP_Text[] playerReadyTexts = new TMP_Text[2];
    [SerializeField] private Button startGameButton = null;

    [SyncVar(hook = nameof(HandleDisplayNameChanged))]
    public string DisplayName = "Загрузка...";
    [SyncVar(hook = nameof(HandleReadyStatusChanged))]
    public bool IsReady = false;

    private bool isLeader;

    public bool IsLeader
    {
        set 
        { 
            isLeader = value;
            startGameButton.gameObject.SetActive(value); // "interactable = value" sounds better
        }
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

    public override void OnStartAuthority()
    {
        CmdSetDisplayName(Authorization.nickname);
        lobbyUI.SetActive(true);
    }

    public override void OnStartClient()
    {
        Room.RoomPlayers.Add(this);
        UpdateDisplay();
    }

    public override void OnStopClient()
    {
        Room.RoomPlayers.Remove(this);
        UpdateDisplay();
    }

    public void HandleReadyStatusChanged(bool oldValue, bool newValue) => UpdateDisplay();

    public void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();

    private void UpdateDisplay() 
    {
        if (!hasAuthority)
        {
            gameObject.SetActive(false);
            foreach (var player in Room.RoomPlayers)
            {
                if (player.hasAuthority)
                {                    
                    player.UpdateDisplay();
                    break;
                }
                
            }
            return;
        }        


        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            playerNameTexts[i].text = "<color=#bbbbbb>Свободное место</color>";
            playerReadyTexts[i].text = string.Empty;
        }

        for (int i = 0; i < Room.RoomPlayers.Count; i++)
        {
            playerNameTexts[i].text = Room.RoomPlayers[i].DisplayName;
            playerReadyTexts[i].text = Room.RoomPlayers[i].IsReady ?
                "<color=#00ff00>Готов</color>" :
                "<color=#ff0000>Не готов</color>";
        }
    }

    public void HandleReadyToStart(bool readyToStart)
    {
        if (!isLeader) 
        {
            return; 
        }
        startGameButton.interactable = readyToStart;
    }

    [Command]
    private void CmdSetDisplayName(string displayName)
    {
        DisplayName = displayName; 
    }

    [Command]
    public void CmdReadyUp()
    {
        IsReady = !IsReady;
        Room.NotifyPlayersOfReadyState();
    }

    public void SwitchBeep() => FindObjectOfType<AudioManager>().Play("SWITCH");

    [Command]
    public void CmdStartGame()
    {
        if (Room.RoomPlayers[0].connectionToClient != connectionToClient)
        {
            return;
        }

        Room.StartGame();
    }
}
