using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Difficulty : MonoBehaviour
{    
    public static int difficultyValue = 1;
    public void OnEnable() => ChooseDifficulty();
    public void ChooseDifficulty()
    {
        bool isMedium = transform.Find("Medium").GetComponent<Toggle>().isOn;
        bool isHigh = transform.Find("High").GetComponent<Toggle>().isOn;
        difficultyValue = isMedium ? 1 : isHigh ? 2 : 0;        
    }
}
