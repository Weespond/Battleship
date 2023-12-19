using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Mirror;


public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject buttons = null;
    //[SerializeField] private Button loadButton = null;
    [SerializeField] private Button playButton = null;
    [SerializeField] private Button onlineButton = null;

    [SerializeField] private GameObject logo = null;
    [SerializeField] private GameObject authField = null;

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
    void Awake() => Application.targetFrameRate = 144;
    
    public void OfflineStartButton()
    {
        GameObject.Find("Main Camera").GetComponent<Animator>().SetTrigger("hasToMoveUp");
        StartCoroutine(StartGameAfterSeconds(1.5f));        
    }
    public void Start() => ReloadMenu();    
    public IEnumerator StartGameAfterSeconds(float count)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
        yield return new WaitForSeconds(count);
        SceneManager.LoadScene("Game_PVE");
    }
    public void BackInMenu() => ReloadMenu();
    public void ReloadMenu()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        buttons.SetActive(true);

        playButton.gameObject.SetActive(true);
        playButton.interactable = Authorization.isAuthorized;
        playButton.gameObject.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = Authorization.isAuthorized ? "Игра с компьютером" : "<color #909090>Игра с компьютером</color>";

        onlineButton.gameObject.SetActive(true);
        onlineButton.interactable = Authorization.isAuthorized;
        onlineButton.gameObject.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = Authorization.isAuthorized ? "Играть по сети" : "<color #909090>Играть по сети</color>";


        authField.SetActive(!Authorization.isAuthorized);

        logo.GetComponent<Image>().color = new Color(255, 255, 255, 0);
        logo.SetActive(true);        
        logo.GetComponent<Animator>().SetBool("isAuthorized", Authorization.isAuthorized);   
        
    }
    public void Quit() => Application.Quit();    
    public void OpenRules() => Application.OpenURL("Rules.html");
    public void OpenAbout() => Application.OpenURL("About.html");
    public void DirtyLoadGame() => Room.DirtyLoadOfflineGame();
}
