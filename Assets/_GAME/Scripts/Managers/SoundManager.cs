using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum SoundType
{
    RIFLE
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : NetworkBehaviour
{
    [SerializeField] private SoundList soundList;
    [SerializeField] private float maxHearingDist;
    [SerializeField] private AnimationCurve volumeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    public static SoundManager Instance;
    private AudioSource _a;
    private GameObject _localPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;
        _a = GetComponent<AudioSource>();
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

    [ClientRpc]
    public void PlaySoundClientRpc(SoundType sound, Vector3 origin)
    {
        // Check distance, if too far, return
        float distance = Vector3.Distance(_localPlayer.transform.position, origin);
        if (distance > maxHearingDist)
            return;

        float t = Mathf.Clamp01(distance / maxHearingDist);
        float volume = volumeCurve.Evaluate(t);

        _a.PlayOneShot(soundList.soundList[(int)sound], volume);
    }
}
