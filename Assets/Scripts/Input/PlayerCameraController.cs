using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Mirror;
using System;

public class PlayerCameraController : NetworkBehaviour
{
    [Header("Camera")]
    
    [SerializeField] private CinemachineVirtualCamera virtualCamera = null;

    public override void OnStartAuthority()
    {
        virtualCamera.gameObject.SetActive(true);

        enabled = true;
    }    
}
