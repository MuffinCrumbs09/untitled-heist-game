using UnityEngine;
using Unity.Netcode;

public class ObjectiveComputerSetter : MonoBehaviour
{
    [Header("Settings")]
    public ObjectiveComputerData data;

    private bool hasSet = false;

    private void Update()
    {
        if (hasSet) return;

        // Check if we've reached the objective/task specified in the data
        if (ObjectiveSystem.Instance != null &&
            ObjectiveSystem.Instance.CurrentObjectiveIndex == data.NextIndex.x)
        {
            var taskComputer = Helper.GetComputerFromTask(data.OriginalIndex.x, data.OriginalIndex.y);
            if (taskComputer == null) { hasSet = true; return; }
            var newTask = ObjectiveSystem.Instance.ObjectiveList[data.NextIndex.x].tasks[data.NextIndex.y];
            if (newTask is not MinigameTask miniTask) { hasSet = true; return; }
            taskComputer.associatedTask = miniTask;

            if (NetworkManager.Singleton.IsServer)
            {
                taskComputer.ResetComputerRpc(data.NewHackTime);
            }
        }
    }
}
