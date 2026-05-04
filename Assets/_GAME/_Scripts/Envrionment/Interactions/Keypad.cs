using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Keypad that progresses objective tasks when interacted with.
/// Can require dependencies before activation.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class Keypad : NetworkBehaviour, IInteractable
{
    [Header("Settings")]
    public string text;
    public int objectiveIndex;
    public int[] taskDependencyIndex;

    /// <summary>
    /// Checks if player can interact based on objective and task dependencies.
    /// </summary>
    public bool CanInteract()
    {
        ObjectiveSystem instance = ObjectiveSystem.Instance;

        // Must match current objective
        if (instance.CurrentObjectiveIndex.Value != objectiveIndex)
            return false;

        // No dependencies = free interaction
        if (taskDependencyIndex == null || taskDependencyIndex.Length == 0)
            return true;

        // Check completed dependencies
        int completed = 0;
        foreach (int index in taskDependencyIndex)
        {
            if (instance.ObjectiveList[objectiveIndex].tasks[index].isCompleted)
                completed++;
        }

        return completed == taskDependencyIndex.Length;
    }

    /// <summary>
    /// Sends interaction request to server.
    /// </summary>
    public void Interact()
    {
        if (!CanInteract()) return;
        InteractServerRpc();
    }

    /// <summary>
    /// Server relays interaction to all clients.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void InteractServerRpc()
    {
        InteractClientRpc();
    }

    /// <summary>
    /// Marks next incomplete task as completed.
    /// </summary>
    [ClientRpc]
    public void InteractClientRpc()
    {
        Objective current = ObjectiveSystem.Instance.GetCurObjective();

        foreach (var task in current.tasks)
        {
            if (task.isCompleted) continue;

            task.isCompleted = true;
            break;
        }
    }

    /// <summary>
    /// Returns UI text.
    /// </summary>
    public string InteractText()
    {
        return CanInteract() ? text : string.Empty;
    }
}