using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class DoorTimer : NetworkBehaviour
{
    [Header("Settings - Door")]
    [SerializeField] private GameObject door;
    [SerializeField] private float doorSpeed = .5f;
    [SerializeField] private Vector3 doorOpen;
    [Header("Settings - Mics")]
    [SerializeField] private float TimeToWait;
    [SerializeField] private TMP_Text Text;

    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float time;
    private bool _hasStarted;
    private Quaternion _doorOpen;

    public void StartTimer() => _hasStarted = true;

    private void Start()
    {
        Text.text = TimeToWait.ToString();
        _doorOpen = Quaternion.Euler(doorOpen.x, doorOpen.y, doorOpen.z);
        isOpen.OnValueChanged += DoorStateChanged;
    }

    private void Update()
    {
        if (_hasStarted)
            TickTimer();
    }

    private void TickTimer()
    {
        if (time >= TimeToWait)
            OpenDoor();

        Text.text = string.Format("{0}", (int)TimeToWait - (int)time);
        time += Time.deltaTime;
    }

    private void OpenDoor()
    {
        Objective cur = ObjectiveSystem.Instance.GetCurObjective();

        foreach (var task in cur.tasks)
        {
            if (task is CustomTask custom)
                if (!custom.isCompleted)
                {
                    custom.CompleteTask();
                    break;
                }
        }

        ToggleDoorServerRpc();

        enabled = false;
    }

    private void DoorStateChanged(bool previousValue, bool newValue)
    {
        StopAllCoroutines();
        StartCoroutine(ToggleDoor(newValue));

        // _obstacle.carving = isOpen.Value;
        // _obstacle.enabled = isOpen.Value;
    }

    private IEnumerator ToggleDoor(bool open)
    {
        if (!open)
            yield break;


        SoundType type = SoundType.DOOR_OPEN;
        SoundManager.Instance.PlaySoundServerRpc(type, transform.position);

        Quaternion startRot = door.transform.rotation;
        Quaternion endRot = _doorOpen;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * doorSpeed;
            door.transform.rotation = Quaternion.Lerp(startRot, endRot, elapsed);
            yield return null;
        }

        door.transform.rotation = endRot;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleDoorServerRpc()
    {
        isOpen.Value = true;
    }
}
