using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Networked vault door that opens/closes based on objectives.
/// Synchronizes state across all clients.
/// </summary>
public class VaultDoor : NetworkBehaviour, IInteractable
{
    [Tooltip("The index of the objective required to open this door."), SerializeField] private int ObjectiveIndex;

    [Tooltip("The speed at which the door opens/closes."), SerializeField] private float openSpeed = 2f;
    [Tooltip("The rotation of the door when open."), SerializeField] private Vector3 doorOpen;
    [Tooltip("The rotation of the door when closed."), SerializeField] private Vector3 doorClosed;

    // Synced door state
    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Quaternion _doorOpen;
    private Quaternion _doorClosed;
    private NavMeshObstacle _obstacle;

    #region Unity Lifecycle
    public override void OnNetworkSpawn()
    {
        // Cache rotations
        _doorOpen = Quaternion.Euler(doorOpen);

        if (doorClosed == Vector3.zero)
            doorClosed = transform.localEulerAngles;

        _doorClosed = Quaternion.Euler(doorClosed);

        _obstacle = GetComponent<NavMeshObstacle>();
        _obstacle.enabled = false;

        // Listen for state changes
        isOpen.OnValueChanged += DoorStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        isOpen.OnValueChanged -= DoorStateChanged;
    }
    #endregion

    /// <summary>
    /// Smoothly rotates the door open/closed.
    /// </summary>
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

    /// <summary>
    /// If player can interact, toggles door state on server.
    /// </summary>
    public void Interact()
    {
        if (!CanInteract()) return;
        ToggleDoorServerRpc();
    }

    /// <summary>
    /// Interact text shown to player when looking at door. Only shows if player can interact.
    /// </summary>
    public string InteractText()
    {
        return CanInteract() ? "Open Vault" : string.Empty;
    }

    /// <summary>
    /// Door can only be opened if correct objective is active and it's closed.
    /// </summary>
    public bool CanInteract()
    {
        return !isOpen.Value &&
               ObjectiveSystem.Instance.CurrentObjectiveIndex.Value == ObjectiveIndex;
    }

    #endregion

    #region Networking

    /// <summary>
    /// Called when door state changes across network.
    /// </summary>
    private void DoorStateChanged(bool previousValue, bool newValue)
    {
        StopAllCoroutines();
        StartCoroutine(ToggleDoor(newValue));
    }

    /// <summary>
    /// Toggles door state on the server.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void ToggleDoorServerRpc()
    {
        isOpen.Value = !isOpen.Value;
    }

    #endregion
}