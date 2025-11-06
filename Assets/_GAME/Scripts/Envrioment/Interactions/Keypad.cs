using UnityEngine;

public class Keypad : MonoBehaviour, IInteractable
{
    [Header("Settings")]
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

        Objective current = ObjectiveSystem.Instance.GetCurObjective();

        foreach(var task in current.tasks)
        {
            if (task.isCompleted) continue;

            task.isCompleted = true;
            break;
        }
    }

    public string InteractText()
    {
        return CanInteract() ? "Click [E] to enter code" : string.Empty;
    }
}
