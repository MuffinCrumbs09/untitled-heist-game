using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public enum OptionsMenus
{
    AUDIO,
    PLAYER
}

public class OptionsMenu : MonoBehaviour
{
    [Header("UI  - Menus")]
    [SerializeField, InspectorName("Tab List")] private GameObject[] menus;
    [Space(10), Header("UI - Buttons")]
    [SerializeField, InspectorName("Audio Button")] private Button audioB;
    [SerializeField, InspectorName("Player Button")] private Button playerB;
    [Space(10), Header("UI - Sliders")]
    [SerializeField, InspectorName("Audio Slider")] private Slider audioS;
    [SerializeField, InspectorName("Music Slider")] private Slider musicS;
    [Space(10), Header("Settings - Misc")]
    [SerializeField, InspectorName("Erase Animator")] private Animator _eraseAnim;
    [SerializeField, InspectorName("Master Audio Mixer")] private AudioMixer _audioMixer;

    private void Start()
    {
        audioB.onClick.AddListener(() => PickTab(OptionsMenus.AUDIO));
        playerB.onClick.AddListener(() => PickTab(OptionsMenus.PLAYER));

        _audioMixer.SetFloat("Music", PlayerPrefs.GetFloat("Music"));
        musicS.value = PlayerPrefs.GetFloat("Music");

        _audioMixer.SetFloat("SFX", PlayerPrefs.GetFloat("SFX"));
        audioS.value = PlayerPrefs.GetFloat("SFX");
    }

    private void PickTab(OptionsMenus menu)
    {
        _eraseAnim.SetTrigger("Erase");

        StartCoroutine(PickTabRoutine(menu));
    }

    private IEnumerator PickTabRoutine(OptionsMenus menu)
    {
        yield return new WaitForSeconds(.5f);

        for (int x = 0; x < menus.Length; x++)
        {
            menus[x].SetActive(false);
        }

        yield return new WaitForSeconds(1f);

        menus[(int)menu].SetActive(true);
    }

    public void UpdateMusicVolume(float volume)
    {
        _audioMixer.SetFloat("Music", volume);
    }

    public void UpdateSFXVolume(float volume)
    {
        _audioMixer.SetFloat("SFX", volume);
    }

    public void SaveVolume()
    {
        _audioMixer.GetFloat("Music", out float musicVolume);
        PlayerPrefs.SetFloat("Music", musicVolume);

        _audioMixer.GetFloat("SFX", out float sfxVolume);
        PlayerPrefs.SetFloat("SFX", sfxVolume);
    }
}
