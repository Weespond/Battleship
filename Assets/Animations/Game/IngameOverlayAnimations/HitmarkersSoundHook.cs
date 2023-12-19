using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitmarkersSoundHook : MonoBehaviour
{    
    [SerializeField] AudioManager audioManager = null;

    public void SoundHit() => audioManager.Play("HIT");

    public void SoundDamage() => audioManager.Play("DAMAGE");

    public void SoundMiss() => audioManager.Play("MISS");

}
