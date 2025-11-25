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

public class SoundManager : NetworkBehaviour
{
    [SerializeField] private SoundList soundList;
    [SerializeField] private SoundList keyboardClickSounds;
    [SerializeField] private float maxHearingDist;
    [SerializeField] private AnimationCurve volumeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Space(10), Header("Audio Sources")]
    [SerializeField] private AudioSource sfxAudio;

    public static SoundManager Instance;
    private GameObject _localPlayer;

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

    private IEnumerator WaitForLocalPlayer()
    {
        // Wait until the local player exists
        while (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            yield return null;
        }

        _localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlaySoundServerRpc(SoundType sound, Vector3 origin)
    {
        PlaySoundClientRpc(sound, origin);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayKeyboardSoundServerRpc(Vector3 origin)
    {
        PlaySoundClientRpc(SoundType.KEYBOARD_CLICK, origin);
    }

    [ClientRpc]
    public void PlaySoundClientRpc(SoundType sound, Vector3 origin)
    {
        // Check distance, if too far, return
        float distance = Vector3.Distance(_localPlayer.transform.position, origin);
        if (distance > maxHearingDist)
            return;

        float t = Mathf.Clamp01(distance / maxHearingDist);
        float volume = volumeCurve.Evaluate(t);


        switch (sound)
        {
            case SoundType.KEYBOARD_CLICK:
                {
                    int randomIndex = Random.Range(0, keyboardClickSounds.soundList.Length);
                    sfxAudio.PlayOneShot(keyboardClickSounds.soundList[randomIndex], volume);
                    break;
                }
            default:
                {
                    sfxAudio.PlayOneShot(soundList.soundList[(int)sound], volume);
                    break;
                }
        }

    }
}
