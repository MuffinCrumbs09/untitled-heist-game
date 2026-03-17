using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class VaultDoor : NetworkBehaviour, IInteractable
{
    public int ObjectiveIndex;

    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private Vector3 doorOpen;
    [SerializeField] private Vector3 doorClosed;
    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

        _obstacle.enabled = false; // isOpen.Value;

        isOpen.OnValueChanged += DoorStateChanged;
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
        if(!CanInteract()) return;
        ToggleDoorServerRpc();
    }

    public string InteractText()
    {
        return CanInteract() ? "Open Vault" : string.Empty;
    }

    public bool CanInteract()
    {
        return !isOpen.Value && ObjectiveSystem.Instance.CurrentObjectiveIndex == ObjectiveIndex;
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

    #endregion
}
