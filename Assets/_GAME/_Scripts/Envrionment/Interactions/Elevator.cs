using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider))]
public class Elevator : NetworkBehaviour, IInteractable
{
    // Network Variables
    public NetworkVariable<bool>    DoorState    = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int>     CurrentFloor = new(0,     NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool>    IsMoving     = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector3> ElevatorPos  = new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Inspector
    [Header("Elevator Settings")]
    [Tooltip("Units per second")]
    public float moveSpeed = 3f;
    public List<Vector3> floorPositions = new();

    [Header("Door Settings")]
    public float doorMoveSpeed = 2f;
    [Tooltip("Index 0 = closed offset, Index 1 = open offset")]
    public Vector3[] doorOffsets = new Vector3[2];
    [Tooltip("Index 0 = left door, Index 1 = right door")]
    public Transform[] doorTransforms = new Transform[2];

    [Header("Timing")]
    public float doorCloseWait  = 1f;
    public float doorOpenDelay  = 0.2f;

    // Private
    private Coroutine _moveCoroutine;
    private Coroutine[] _doorCoroutines = new Coroutine[2];


    #region Unity / Network Events

    public override void OnNetworkSpawn()
    {
        if(IsServer)
            ElevatorPos.Value = transform.localPosition;

        transform.localPosition = ElevatorPos.Value;

        ElevatorPos.OnValueChanged  += OnElevatorPosSynced;
        DoorState.OnValueChanged    += OnDoorStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        ElevatorPos.OnValueChanged  -= OnElevatorPosSynced;
        DoorState.OnValueChanged    -= OnDoorStateChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !other.CompareTag("Player")) return;

        if (other.TryGetComponent(out NetworkObject netObj))
            netObj.TrySetParent(NetworkObject, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer || !other.CompareTag("Player")) return;

        if (other.TryGetComponent(out NetworkObject netObj))
            netObj.TryRemoveParent(true);
    }

    #endregion

    #region Callbacks

    // Runs on ALL clients — keeps visual position in sync
    private void OnElevatorPosSynced(Vector3 _, Vector3 newPos)
    {
        transform.localPosition = newPos;
    }

    // Runs on ALL clients — drives door animations
    private void OnDoorStateChanged(bool _, bool open)
    {
        SetDoorCoroutine(0, open, isLeftDoor: true);
        SetDoorCoroutine(1, open, isLeftDoor: false);
    }

    #endregion


    #region Coroutines

    private void SetDoorCoroutine(int index, bool open, bool isLeftDoor)
    {
        if (_doorCoroutines[index] != null)
            StopCoroutine(_doorCoroutines[index]);

        _doorCoroutines[index] = StartCoroutine(AnimateDoor(doorTransforms[index], open, isLeftDoor));
    }

    private IEnumerator AnimateDoor(Transform door, bool open, bool isLeftDoor)
    {
        SoundType sound = open ? SoundType.DOOR_OPEN : SoundType.DOOR_CLOSED;
        SoundManager.Instance.PlaySoundServerRpc(sound, transform.position);

        Vector3 start  = door.localPosition;
        Vector3 target = doorOffsets[open ? 1 : 0];
        if (isLeftDoor) target.x = -target.x;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * doorMoveSpeed;
            door.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }

        door.localPosition = target;
    }

    // Server-only
    private IEnumerator MoveElevatorRoutine(int targetFloor)
    {
        IsMoving.Value = true;

        // Close doors before moving
        if (DoorState.Value)
        {
            DoorState.Value = false;
            yield return new WaitForSeconds(doorCloseWait);
        }

        Vector3 targetPos = floorPositions[targetFloor];

        while (true)
        {
            Vector3 next = Vector3.MoveTowards(ElevatorPos.Value, targetPos, moveSpeed * Time.deltaTime);
            ElevatorPos.Value = next;              // synced to all clients via NetworkVariable

            if (Vector3.SqrMagnitude(next - targetPos) < 0.0001f)
                break;

            yield return null;
        }

        ElevatorPos.Value  = targetPos;
        CurrentFloor.Value = targetFloor;

        yield return new WaitForSeconds(doorOpenDelay);

        DoorState.Value = true;
        IsMoving.Value  = false;
    }

    #endregion


    #region IInteractable
    public bool CanInteract() => !IsMoving.Value;

    public void Interact()
    {
        bool inside = NetworkManager.LocalClient.PlayerObject.transform.IsChildOf(NetworkObject.transform);

        if (inside)
        {
            int next = (CurrentFloor.Value + 1) % floorPositions.Count;
            MoveToFloorServerRpc(next);
        }
        else
        {
            ToggleDoorsServerRpc();
        }
    }

    public string InteractText()
    {
        bool inside = NetworkManager.LocalClient.PlayerObject.transform.IsChildOf(NetworkObject.transform);
        return inside ? "Next Floor" : "Toggle Doors";
    }

    #endregion

    #region RPCs

    [Rpc(SendTo.Server)]
    public void ToggleDoorsServerRpc()
    {
        if (!IsMoving.Value)
            DoorState.Value = !DoorState.Value;
    }

    [Rpc(SendTo.Server)]
    public void MoveToFloorServerRpc(int floor)
    {
        if (IsMoving.Value) return;

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(MoveElevatorRoutine(floor));
    }

    #endregion
}