using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using System;

[RequireComponent(typeof(NetworkObject)),
RequireComponent(typeof(Collider))]
public class Elevator : NetworkBehaviour, IInteractable
{
    public NetworkVariable<bool> DoorState = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> currentFloor = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isMoving = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Elevator Settings")]
    public float moveSpeed = .1f;
    public List<Vector3> floorPositions = new();
    [Header("Door Settings")]
    public float doorMoveSpeed = 2f;
    [Tooltip("0 = closed, 1 = open")]
    public Vector3[] doorOffsets = new Vector3[2];
    [Tooltip("0 = left door, 1 = right door")]
    public Transform[] doorTransforms = new Transform[2]; // 0 = left door, 1 = right door
    [Header("Collider Settings")]
    public Collider triggerCollider;

    #region Unity Events
    private void Start()
    {
        DoorState.OnValueChanged += DoorStateChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            if (playerNetObj != null)
            {
                playerNetObj.TrySetParent(NetworkObject, true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            if (playerNetObj != null)
            {
                playerNetObj.TryRemoveParent(true);
            }
        }
    }
    #endregion

    #region Functions
    private IEnumerator ToggleDoor(Transform doorTransform, bool open, bool isLeftDoor)
    {
        SoundType type = open ? SoundType.DOOR_OPEN : SoundType.DOOR_CLOSED;
        SoundManager.Instance.PlaySoundServerRpc(type, transform.position);

        Vector3 startPos = doorTransform.localPosition;
        Vector3 endPos = doorOffsets[open ? 1 : 0];

        if (isLeftDoor)
            endPos.x = -endPos.x;

        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * doorMoveSpeed;
            doorTransform.localPosition = Vector3.Lerp(startPos, endPos, elapsed);
            yield return null;
        }

        doorTransform.localPosition = endPos;
    }

    private IEnumerator ServerMoveElevator(int targetFloor)
    {
        isMoving.Value = true;

        // Close doors first
        if (DoorState.Value)
        {
            DoorState.Value = false;
            yield return new WaitForSeconds(1f);
        }

        Vector3 targetPosition = floorPositions[targetFloor];

        while (Vector3.Distance(transform.localPosition, targetPosition) > 0.01f)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.localPosition = targetPosition;

        currentFloor.Value = targetFloor;

        yield return new WaitForSeconds(0.2f);

        // Open doors
        DoorState.Value = true;

        isMoving.Value = false;
    }
    #endregion

    #region Calllbacks
    private void DoorStateChanged(bool previous, bool current)
    {
        StartCoroutine(ToggleDoor(doorTransforms[0], current, true));
        StartCoroutine(ToggleDoor(doorTransforms[1], current, false));
    }
    #endregion

    #region Interface
    public bool CanInteract()
    {
        return isMoving.Value == false;
    }

    public void Interact()
    {
        bool inside = NetworkManager.LocalClient.PlayerObject.transform.IsChildOf(NetworkObject.transform);

        if (inside)
        {
            int nextFloor = (currentFloor.Value + 1) % floorPositions.Count;
            MoveToFloorServerRpc(nextFloor);
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

    #region Networking
    [Rpc(SendTo.Server)]
    public void ToggleDoorsServerRpc()
    {
        DoorState.Value = !DoorState.Value;
    }

    [Rpc(SendTo.Server)]
    public void MoveToFloorServerRpc(int floor)
    {
        if (isMoving.Value) return;

        StartCoroutine(ServerMoveElevator(floor));
    }
    #endregion
}