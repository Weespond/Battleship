using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuCamera : MonoBehaviour
{       
    public void PlayGame() => SceneManager.LoadScene("Game_PVE");
}
