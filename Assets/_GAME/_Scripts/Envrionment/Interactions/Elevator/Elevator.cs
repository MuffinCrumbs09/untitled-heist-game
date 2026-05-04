using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// A networked elevator system that manages movement between floors, 
/// door animations, and player parenting.
/// </summary>
public class Elevator : NetworkBehaviour, IInteractable
{
    [Header("Network Sync")]
    public NetworkVariable<bool> DoorState = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> CurrentFloor = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsMoving = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector3> ElevatorPos = new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Elevator Movement")]
    public float moveSpeed = 3f;
    public List<Vector3> floorPositions = new();

    [Header("Door Configuration")]
    public float doorMoveSpeed = 2f;
    public Vector3[] doorOffsets = new Vector3[2]; // 0: Closed, 1: Open
    public Transform[] doorTransforms = new Transform[2]; // 0: Left, 1: Right

    private Coroutine _moveCoroutine;
    private Coroutine[] _doorCoroutines = new Coroutine[2];

    public override void OnNetworkSpawn()
    {
        if (IsServer) ElevatorPos.Value = transform.localPosition;
        
        transform.localPosition = ElevatorPos.Value;
        ElevatorPos.OnValueChanged += (oldV, newV) => transform.localPosition = newV;
        DoorState.OnValueChanged += (oldS, newS) => ToggleDoorsLocal(newS);
    }

    /// <summary>
    /// Parents the player to the elevator on the server so they move with it.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (IsServer && other.CompareTag("Player") && other.TryGetComponent(out NetworkObject netObj))
            netObj.TrySetParent(NetworkObject, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsServer && other.CompareTag("Player") && other.TryGetComponent(out NetworkObject netObj))
            netObj.TryRemoveParent(true);
    }

    #region Interface

    public bool CanInteract() => !IsMoving.Value;

    public void Interact()
    {
        // Check if player is inside the elevator by checking parenting
        bool isInside = NetworkManager.LocalClient.PlayerObject.transform.IsChildOf(transform);

        if (isInside)
        {
            int next = (CurrentFloor.Value + 1) % floorPositions.Count;
            MoveToFloorServerRpc(next);
        }
        else ToggleDoorsServerRpc();
    }

    public string InteractText()
    {
        if (!CanInteract()) return string.Empty;
        bool isInside = NetworkManager.LocalClient.PlayerObject.transform.IsChildOf(transform);
        return isInside ? "Next Floor" : "Toggle Doors";
    }

    #endregion

    #region Movement & Doors

    private void ToggleDoorsLocal(bool open)
    {
        for (int i = 0; i < doorTransforms.Length; i++)
        {
            if (_doorCoroutines[i] != null) StopCoroutine(_doorCoroutines[i]);
            _doorCoroutines[i] = StartCoroutine(AnimateDoor(doorTransforms[i], open, i == 0));
        }
    }

    private IEnumerator AnimateDoor(Transform door, bool open, bool isLeft)
    {
        SoundType sound = open ? SoundType.DOOR_OPEN : SoundType.DOOR_CLOSED;
        SoundManager.Instance.PlaySoundServerRpc(sound, transform.position);

        Vector3 target = doorOffsets[open ? 1 : 0];
        if (isLeft) target.x = -target.x;

        while (Vector3.Distance(door.localPosition, target) > 0.01f)
        {
            door.localPosition = Vector3.MoveTowards(door.localPosition, target, doorMoveSpeed * Time.deltaTime);
            yield return null;
        }
        door.localPosition = target;
    }

    [Rpc(SendTo.Server)]
    public void MoveToFloorServerRpc(int floor)
    {
        if (IsMoving.Value) return;
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(MoveRoutine(floor));
    }

    private IEnumerator MoveRoutine(int targetFloor)
    {
        IsMoving.Value = true;

        // Ensure doors are closed before movement
        if (DoorState.Value)
        {
            DoorState.Value = false;
            yield return new WaitForSeconds(1f);
        }

        Vector3 targetPos = floorPositions[targetFloor];
        while (Vector3.Distance(ElevatorPos.Value, targetPos) > 0.001f)
        {
            ElevatorPos.Value = Vector3.MoveTowards(ElevatorPos.Value, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        CurrentFloor.Value = targetFloor;
        yield return new WaitForSeconds(0.2f);
        DoorState.Value = true;
        IsMoving.Value = false;
    }

    [Rpc(SendTo.Server)]
    public void ToggleDoorsServerRpc() { if (!IsMoving.Value) DoorState.Value = !DoorState.Value; }

    #endregion
}