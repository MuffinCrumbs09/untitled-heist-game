using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public enum SoundType
{
    RIFLE,

    HACK_COMPLETE,
    DOOR_OPEN,
    DOOR_CLOSED,
    // Multiple Sounds
    KEYBOARD_CLICK,
}

/// <summary>
/// Handles all sound playback across the network.
/// Includes distance-based volume attenuation.
/// </summary>
public class SoundManager : NetworkBehaviour
{
    #region Inspector

    [Header("Sound Libraries")]
    [Tooltip("General sounds mapped by enum.")]
    [SerializeField] private SoundList soundList;

    [Tooltip("Random keyboard click variations.")]
    [SerializeField] private SoundList keyboardClickSounds;

    [Header("Audio Settings")]
    [Tooltip("Maximum distance a sound can be heard.")]
    [SerializeField] private float maxHearingDist;

    [Tooltip("Controls volume falloff over distance.")]
    [SerializeField] private AnimationCurve volumeCurve;

    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxAudio;

    #endregion

    #region Singleton
    public static SoundManager Instance;
    #endregion

    #region Private Fields
    private GameObject localPlayer;
    #endregion

    #region Unity Events
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(WaitForLocalPlayer());
    }
    #endregion

    #region Setup

    /// <summary>
    /// Waits until the local player object is available.
    /// </summary>
    private IEnumerator WaitForLocalPlayer()
    {
        while (NetworkManager.Singleton?.LocalClient?.PlayerObject == null)
            yield return null;

        localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
    }

    #endregion

    #region Networking API

    /// <summary>
    /// Requests a sound to be played across all clients.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlaySoundServerRpc(SoundType sound, Vector3 origin)
    {
        PlaySoundClientRpc(sound, origin);
    }

    /// <summary>
    /// Shortcut for keyboard sounds.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayKeyboardSoundServerRpc(Vector3 origin)
    {
        PlaySoundClientRpc(SoundType.KEYBOARD_CLICK, origin);
    }

    /// <summary>
    /// Plays sound locally on each client with distance-based volume.
    /// </summary>
    [ClientRpc]
    private void PlaySoundClientRpc(SoundType sound, Vector3 origin)
    {
        float distance = Vector3.Distance(localPlayer.transform.position, origin);

        if (distance > maxHearingDist)
            return;

        float t = Mathf.Clamp01(distance / maxHearingDist);
        float volume = volumeCurve.Evaluate(t);

        switch (sound)
        {
            case SoundType.KEYBOARD_CLICK:
                int index = Random.Range(0, keyboardClickSounds.soundList.Length);
                sfxAudio.PlayOneShot(keyboardClickSounds.soundList[index], volume);
                break;

            default:
                sfxAudio.PlayOneShot(soundList.soundList[(int)sound], volume);
                break;
        }
    }

    #endregion
}