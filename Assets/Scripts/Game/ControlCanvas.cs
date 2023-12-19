using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlCanvas : MonoBehaviour // basically - placement control canvas*
{
    [SerializeField] private ShipsGrid grid = null;

    private Button startButton = null;
    private GameObject autoPlacementPanel = null;
    private GameObject infoPanel = null;
    private GameObject panel = null;    

    void Awake()
    {
        infoPanel = transform.Find("InfoPanel").gameObject;
        infoPanel.SetActive(false);

        autoPlacementPanel = transform.Find("Panel").Find("AutoPlacementPanel").gameObject;
        autoPlacementPanel.SetActive(false);

        panel = transform.Find("Panel").gameObject;

        startButton = panel.transform.Find("Start").gameObject.GetComponent<Button>();

    }

    public void ToggleAutoPlacementPanel()
    {
        if (!autoPlacementPanel.activeSelf)
            autoPlacementPanel.SetActive(true);
        else
            autoPlacementPanel.SetActive(false);
    }

    public void ToggleReadyButton(bool isReady)
    {
        if (!isReady)
        {
            startButton.interactable = false;
            startButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "<color=grey>Начать игру</color>";
        }
        else
        {
            startButton.interactable = true;
            startButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "<color=white>Начать игру</color>";
        }
    }

    public void P_Random() => grid.AutoPlacement_Random();   
    public void P_Anti_Diagonal() => grid.AutoPlacement_AntiDiagonal();
    public void P_Coasts() => grid.AutoPlacement_Coasts();
    public void P_Edges() => grid.AutoPlacement_OnEdges();

    void Update()
    {
        if (panel.activeSelf) 
        {
            infoPanel.SetActive(grid.IsDragging);
            ToggleReadyButton(grid.IsReadyToStart);
        }        
    }    
}
