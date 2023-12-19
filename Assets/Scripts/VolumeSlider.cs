using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class VolumeSlider : MonoBehaviour
{
    public AudioMixer mixer;
    public void SetMasterVolume(float sliderValue) => mixer.SetFloat("Master", Mathf.Log10(sliderValue) * 20);

}
