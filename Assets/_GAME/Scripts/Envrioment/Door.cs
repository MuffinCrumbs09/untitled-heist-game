using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Services.Matchmaker.Models;
using System.Diagnostics;

public class Door : NetworkBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string interactionText = "Door";
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private Vector3 doorOpen;
    [SerializeField] private Vector3 doorClosed;

    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Quaternion _doorOpen;
    private Quaternion _doorClosed;
    public void Start()
    {
        _doorOpen = Quaternion.Euler(doorOpen.x, doorOpen.y, doorOpen.z);
        _doorClosed = Quaternion.Euler(doorClosed.x, doorClosed.y, doorClosed.z);
        isOpen.OnValueChanged += DoorStateChanged;
    }

    public override void OnDestroy()
    {
        isOpen.OnValueChanged -= DoorStateChanged;
    }

    private IEnumerator ToggleDoor(bool open)
    {
        Quaternion startRot = transform.rotation;
        Quaternion endRot = open ? _doorOpen : _doorClosed;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * openSpeed;
            transform.rotation = Quaternion.Lerp(startRot, endRot, elapsed);
            yield return null;
        }

        transform.rotation = endRot;
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

    #endregion


    #region Networking
    private void DoorStateChanged(bool previousValue, bool newValue)
    {
        StopAllCoroutines();
        StartCoroutine(ToggleDoor(newValue));
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleDoorServerRpc()
    {
        isOpen.Value = !isOpen.Value;
    }
    #endregion
}
