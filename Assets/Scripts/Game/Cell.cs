using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class Cell : NetworkBehaviour
{
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

    public void ShootCell(Button context) => OGM.ShootCell(context);
    
}
