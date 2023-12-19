using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class OnlineLobbyMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField ipAddressInputField = null;
    [SerializeField] private Text hostIpAddressText = null;
    [SerializeField] private TMP_Text errorInfoText = null;
    [SerializeField] private GameObject caption = null;

    [Header("Buttons")]
    [SerializeField] private Button joinButton = null;
    [SerializeField] private Button hostButton = null;
    [SerializeField] private Button discButton = null;
    [SerializeField] private Button closeHostButton = null;
    [SerializeField] private Button backToMenuButton = null;    

    float lastTimeTriedToConnect;

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

    private void OnEnable()
    {
        Room.OnClientConnectedToLobby += HandleClientConnected;
        Room.OnClientDisconnectedFromLobby += HandleClientDisconnected;
        Room.OnClientHostedTheServer += HandleClientHostedServer;        

        errorInfoText.text = "";

        joinButton.gameObject.SetActive(true);
        discButton.gameObject.SetActive(false);
        hostButton.gameObject.SetActive(true);
        closeHostButton.gameObject.SetActive(false);

        backToMenuButton.gameObject.SetActive(true);
    }

    void ToggleHostCloseConn()
    {
        hostButton.gameObject.SetActive(!hostButton.gameObject.activeSelf);
        closeHostButton.gameObject.SetActive(!closeHostButton.gameObject.activeSelf);
    }
    void ToggleJoinDisconnect()
    {
        joinButton.gameObject.SetActive(!joinButton.gameObject.activeSelf);
        discButton.gameObject.SetActive(!discButton.gameObject.activeSelf);
    }

    public void HostLobby()
    {
        errorInfoText.text = "";

        try
        {
            Room.StartHost();

            string myIp = new WebClient().DownloadString("http://icanhazip.com");
            hostIpAddressText.text = "IP адрес сессии \n" + myIp;

            joinButton.interactable = false;
            backToMenuButton.interactable = false;
            caption.SetActive(false);
            ToggleHostCloseConn();
        }
        catch
        {
            joinButton.gameObject.SetActive(true);
            discButton.gameObject.SetActive(false);
            hostButton.gameObject.SetActive(true);
            closeHostButton.gameObject.SetActive(false);

            caption.SetActive(true);

            return;
        }
    }
    public void JoinLobby()
    {
        errorInfoText.text = "";
        string ipAddress = ipAddressInputField.text;
        
        if (string.IsNullOrEmpty(ipAddress)) // validate ip here
        {
            errorInfoText.text = "<color=#AA0000>Введите IP адрес </color>";
            return;
        }
        else if(!IsIPAddress(ipAddress) && ipAddress != "localhost")
        {
            errorInfoText.text = "<color=#AA0000>Некорректный IP адрес </color>";
            return;
        }

        Room.networkAddress = ipAddress;
        Room.StartClient();

        lastTimeTriedToConnect = Time.time;
        
        ToggleJoinDisconnect();
        hostButton.interactable = false;
        discButton.interactable = false;

        caption.SetActive(false);

        backToMenuButton.interactable = false;
    }

    public void CloseConn()
    {
        Room.StopHost();

        hostIpAddressText.text = "";

        joinButton.interactable = true;
        backToMenuButton.interactable = true;

        caption.SetActive(true);

        ToggleHostCloseConn();
    }
    public void Disconnect()
    {
        lastTimeTriedToConnect = 0f;
        Room.StopClient();        
    }

    private void HandleClientHostedServer()
    {
        joinButton.interactable = false;
        discButton.interactable = true;
        backToMenuButton.interactable = false;
    }

    private void HandleClientConnected()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            lastTimeTriedToConnect = 0f;

            caption.SetActive(false);

            joinButton.interactable = true;
            discButton.interactable = true;
            backToMenuButton.interactable = false;
        }
    }
    private void HandleClientDisconnected()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            TimeOutCheck();

            hostButton.interactable = true;
            joinButton.interactable = true;
            discButton.interactable = true;

            caption.SetActive(true);

            backToMenuButton.interactable = true;

            joinButton.gameObject.SetActive(true);
            discButton.gameObject.SetActive(false);
            hostButton.gameObject.SetActive(true);
            closeHostButton.gameObject.SetActive(false);
        }
    }

    bool IsIPAddress(string ipAddress)
    {
        string[] strArray = ipAddress.Split('.');
        if (strArray.Length != 4)
        {
            return false;
        }
        for (int i = 0; i < 4; i++)
        {
            int val;
            if (!int.TryParse(strArray[i], out val))
            {
                return false;
            }
            val = int.Parse(strArray[i]);
            if (val < 0 || val > 255)
            {
                return false;
            }

        }
        return true;
    }

    void TimeOutCheck()
    {
        bool isTimeout = true;
        float delta = Time.time - lastTimeTriedToConnect;

        isTimeout &= lastTimeTriedToConnect != 0f;
        isTimeout &= delta > 10f && delta < 10.1f;
                
        if (isTimeout)
        {
            errorInfoText.text = "<color=#AA0000>Ошибка подключения</color>";
            return;
        }
        
        if ((delta > 10.1f || delta < 10f) && lastTimeTriedToConnect != 0f)
        {
            errorInfoText.text = "<color=#990000>Хост остановил сессию</color>";
            return;
        }
        
    }
}
