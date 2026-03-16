using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Services.Matchmaker.Models;
using System.Diagnostics;
using UnityEngine.AI;

[
    RequireComponent(typeof(NavMeshObstacle))]
public class Door : NetworkBehaviour, IInteractable, IReady
{
    [Header("Settings")]
    [SerializeField] private string interactionText = "Door";
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private Vector3 doorOpen;
    [SerializeField] private Vector3 doorClosed;
    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    bool isReady = false;

    private Quaternion _doorOpen;
    private Quaternion _doorClosed;
    private NavMeshObstacle _obstacle;
    public override void OnNetworkSpawn()
    {
        _doorOpen = Quaternion.Euler(doorOpen.x, doorOpen.y, doorOpen.z);

        if (doorClosed == Vector3.zero)
            doorClosed = transform.localEulerAngles;

        _doorClosed = Quaternion.Euler(doorClosed.x, doorClosed.y, doorClosed.z);
        _obstacle = GetComponent<NavMeshObstacle>();

        // _obstacle.carveOnlyStationary = false;
        //  _obstacle.carving = isOpen.Value;
        _obstacle.enabled = false; // isOpen.Value;

        isOpen.OnValueChanged += DoorStateChanged;
        isReady = true;
    }

    public override void OnNetworkDespawn()
    {
        isOpen.OnValueChanged -= DoorStateChanged;
    }

    private IEnumerator ToggleDoor(bool open)
    {
        SoundType type = open ? SoundType.DOOR_OPEN : SoundType.DOOR_CLOSED;
        SoundManager.Instance.PlaySoundServerRpc(type, transform.position);

        Quaternion startRot = transform.localRotation;
        Quaternion endRot = open ? _doorOpen : _doorClosed;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * openSpeed;
            transform.localRotation = Quaternion.Lerp(startRot, endRot, elapsed);
            yield return null;
        }

        transform.localRotation = endRot;
    }

    #region Interface
    public void Interact()
    {
        ToggleDoorServerRpc();
    }

    public string InteractText()
    {
        return isOpen.Value ? string.Format("Close {0}", interactionText) : string.Format("Open: {0}", interactionText);
    }

    // Currently Unused
    public bool CanInteract()
    {
        return true;
    }
    #endregion


    #region Networking
    private void DoorStateChanged(bool previousValue, bool newValue)
    {
        StopAllCoroutines();
        StartCoroutine(ToggleDoor(newValue));

        // _obstacle.carving = isOpen.Value;
        // _obstacle.enabled = isOpen.Value;
    }

    [Rpc(SendTo.Server)]
    public void ToggleDoorServerRpc()
    {
        isOpen.Value = !isOpen.Value;
    }

    public bool IsReady()
    {
        return isReady;
    }
    #endregion
}
