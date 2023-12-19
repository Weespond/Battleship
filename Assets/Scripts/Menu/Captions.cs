using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Captions : MonoBehaviour
{
    [SerializeField] GameObject low = null;
    [SerializeField] GameObject medium = null;
    [SerializeField] GameObject high = null;
        
    public void ShowCaption()
    { 
        switch (Difficulty.difficultyValue)
        {
            case 0:
                {
                    low.SetActive(true);
                    medium.SetActive(false);
                    high.SetActive(false);
                    break;
                }
            case 1:
                {
                    low.SetActive(false);
                    medium.SetActive(true);
                    high.SetActive(false);
                    break;
                }
            case 2:
                {
                    low.SetActive(false);
                    medium.SetActive(false);
                    high.SetActive(true);
                    break;
                }
        }            
    }
}
