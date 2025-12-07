using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [Header("Audio Mixer Reference")]
    public AudioMixer audioMixer; // assign MainMixer here
    public Slider volumeSlider;   // assign your UI slider

    [Header("Exposed Parameter Name")]
    public string volumeParameter = "MusicVolume"; // or "SFXVolume"

    private void Start()
    {
        // Load previous saved volume or default to 0 dB (full volume)
        float savedVolume = PlayerPrefs.GetFloat(volumeParameter, 0f);
        audioMixer.SetFloat(volumeParameter, savedVolume);

        // Convert from decibels to slider value (assuming -80 to 0 range)
        volumeSlider.value = Mathf.InverseLerp(-80f, 0f, savedVolume);

        // Listen for slider changes
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        // Convert slider (0–1) to dB (-80 to 0)
        float dB = Mathf.Lerp(-80f, 0f, value);
        audioMixer.SetFloat(volumeParameter, dB);

        // Save to PlayerPrefs
        PlayerPrefs.SetFloat(volumeParameter, dB);
    }
}
