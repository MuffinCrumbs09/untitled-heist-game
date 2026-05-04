using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Enum for different categories within the options menu.
/// </summary>
public enum OptionsMenus { AUDIO, PLAYER }

/// <summary>
/// Controls the options menu UI, tab switching, and audio volume persistence.
/// </summary>
public class OptionsMenu : MonoBehaviour
{
    #region Serialized Fields

    [Header("UI - Menus")]
    [SerializeField, InspectorName("Tab List")] 
    [Tooltip("Array of GameObjects representing different option tabs.")]
    private GameObject[] menus;

    [Space(10), Header("UI - Buttons")]
    [SerializeField, InspectorName("Audio Button")] 
    [Tooltip("Button used to switch to the Audio tab.")]
    private Button audioB;

    [SerializeField, InspectorName("Player Button")] 
    [Tooltip("Button used to switch to the Player tab.")]
    private Button playerB;

    [Space(10), Header("UI - Sliders")]
    [SerializeField, InspectorName("Audio Slider")] 
    [Tooltip("Slider controlling the SFX volume.")]
    private Slider audioS;

    [SerializeField, InspectorName("Music Slider")] 
    [Tooltip("Slider controlling the Music volume.")]
    private Slider musicS;

    [Space(10), Header("Settings - Misc")]
    [SerializeField, InspectorName("Erase Animator")] 
    [Tooltip("Animator used for tab transition effects.")]
    private Animator _eraseAnim;

    [SerializeField, InspectorName("Master Audio Mixer")] 
    [Tooltip("The AudioMixer asset to apply volume changes to.")]
    private AudioMixer _audioMixer;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes button listeners and loads saved audio settings from PlayerPrefs.
    /// </summary>
    private void Start()
    {
        audioB.onClick.AddListener(() => PickTab(OptionsMenus.AUDIO));
        playerB.onClick.AddListener(() => PickTab(OptionsMenus.PLAYER));

        _audioMixer.SetFloat("Music", PlayerPrefs.GetFloat("Music"));
        musicS.value = PlayerPrefs.GetFloat("Music");

        _audioMixer.SetFloat("SFX", PlayerPrefs.GetFloat("SFX"));
        audioS.value = PlayerPrefs.GetFloat("SFX");
    }

    #endregion

    #region Tab Management

    /// <summary>
    /// Triggers the transition animation and starts the tab switching coroutine.
    /// </summary>
    private void PickTab(OptionsMenus menu)
    {
        _eraseAnim.SetTrigger("Erase");
        StartCoroutine(PickTabRoutine(menu));
    }

    /// <summary>
    /// Logic for disabling all tabs and enabling the selected one after a delay.
    /// </summary>
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

    #endregion

    #region Audio Controls

    /// <summary> Updates the mixer's music parameter. </summary>
    public void UpdateMusicVolume(float volume) => _audioMixer.SetFloat("Music", volume);

    /// <summary> Updates the mixer's SFX parameter. </summary>
    public void UpdateSFXVolume(float volume) => _audioMixer.SetFloat("SFX", volume);

    /// <summary>
    /// Saves current mixer volume values into PlayerPrefs for persistence.
    /// </summary>
    public void SaveVolume()
    {
        _audioMixer.GetFloat("Music", out float musicVolume);
        PlayerPrefs.SetFloat("Music", musicVolume);

        _audioMixer.GetFloat("SFX", out float sfxVolume);
        PlayerPrefs.SetFloat("SFX", sfxVolume);
    }

    #endregion
}