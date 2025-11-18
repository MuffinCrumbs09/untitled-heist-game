using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Keypad : NetworkBehaviour, IInteractable
{
    [Header("Settings")]
    public string text;
    public int objectiveIndex;
    public int[] taskDependencyIndex;

    public bool CanInteract()
    {
        bool objective;
        ObjectiveSystem instance = ObjectiveSystem.Instance;

        objective = instance.CurrentObjectiveIndex == objectiveIndex;
        if (!objective) return false;
        if (objective && (taskDependencyIndex.Length == 0 || taskDependencyIndex == null)) return true;


        int tasks = 0;
        foreach (int index in taskDependencyIndex)
            if (instance.ObjectiveList[objectiveIndex].tasks[index].isCompleted)
                tasks++;

        return tasks == taskDependencyIndex.Length;
    }

    public void Interact()
    {
        if (!CanInteract()) return;
        InteractServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void InteractServerRpc()
    {
        InteractClientRpc();
    }

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

    public string InteractText()
    {
        return CanInteract() ? text : string.Empty;
    }
}
