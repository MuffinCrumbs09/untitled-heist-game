using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Services.Matchmaker.Models;
using System.Diagnostics;
using UnityEngine.AI;

[RequireComponent(typeof(NetworkObject)),
    RequireComponent(typeof(NavMeshObstacle))]
public class Door : NetworkBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string interactionText = "Door";
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private Vector3 doorOpen;
    [SerializeField] private Vector3 doorClosed;
    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Quaternion _doorOpen;
    private Quaternion _doorClosed;
    private NavMeshObstacle _obstacle;
    public void Start()
    {
        _doorOpen = Quaternion.Euler(doorOpen.x, doorOpen.y, doorOpen.z);
        _doorClosed = Quaternion.Euler(doorClosed.x, doorClosed.y, doorClosed.z);
        _obstacle = GetComponent<NavMeshObstacle>();

        // _obstacle.carveOnlyStationary = false;
        //  _obstacle.carving = isOpen.Value;
        _obstacle.enabled = false; // isOpen.Value;

        isOpen.OnValueChanged += DoorStateChanged;
    }

    public override void OnDestroy()
    {
        isOpen.OnValueChanged -= DoorStateChanged;
    }

    private IEnumerator ToggleDoor(bool open)
    {
        SoundType type = open ? SoundType.DOOR_OPEN : SoundType.DOOR_CLOSED;
        SoundManager.Instance.PlaySoundServerRpc(type, transform.position);

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

    [ServerRpc(RequireOwnership = false)]
    public void ToggleDoorServerRpc()
    {
        isOpen.Value = !isOpen.Value;
    }
    #endregion
}
