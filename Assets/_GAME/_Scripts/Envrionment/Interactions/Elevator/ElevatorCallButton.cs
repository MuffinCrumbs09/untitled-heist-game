using UnityEngine;
using Unity.Netcode;

public class ElevatorCallButton : NetworkBehaviour, IInteractable
{
    [Header("References")]
    public Elevator elevator;

    [Tooltip("Which floor this button belongs to")]
    public int floorIndex;

    public bool CanInteract()
    {
        if (elevator == null) return false;

        return !elevator.IsMoving.Value && elevator.CurrentFloor.Value != floorIndex;
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        CallElevatorServerRpc(floorIndex);
    }

    public string InteractText()
    {
        if (elevator == null) return "No Elevator";

        if (elevator.CurrentFloor.Value == floorIndex || elevator.IsMoving.Value)
            return string.Empty;

        return "Call Elevator";
    }

    [Rpc(SendTo.Server)]
    private void CallElevatorServerRpc(int floor)
    {
        if (elevator == null) return;

        // Prevent spam / invalid calls
        if (elevator.IsMoving.Value) return;
        if (elevator.CurrentFloor.Value == floor) return;

        elevator.MoveToFloorServerRpc(floor);
    }
}