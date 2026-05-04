using UnityEngine;
using Unity.Netcode;

/// <summary>
/// A button located outside an elevator used to call it to a specific floor.
/// </summary>
public class ElevatorCallButton : NetworkBehaviour, IInteractable
{
    [Header("References")]
    public Elevator elevator;
    [Tooltip("The floor index this button represents.")] public int floorIndex;

    public bool CanInteract()
    {
        if (elevator == null) return false;
        // Cannot interact if moving or if the elevator is already here
        return !elevator.IsMoving.Value && elevator.CurrentFloor.Value != floorIndex;
    }

    public void Interact()
    {
        if (CanInteract()) CallElevatorServerRpc(floorIndex);
    }

    public string InteractText()
    {
        if (elevator == null) return "No Elevator Link";
        return CanInteract() ? "Call Elevator" : string.Empty;
    }

    [Rpc(SendTo.Server)]
    private void CallElevatorServerRpc(int floor)
    {
        if (elevator != null && !elevator.IsMoving.Value && elevator.CurrentFloor.Value != floor)
            elevator.MoveToFloorServerRpc(floor);
    }
}