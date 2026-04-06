using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Watches for a specific objective/task to become active, then rewires a
/// Computer's associated MinigameTask to a new one. Server-only logic.
/// </summary>
public class ObjectiveComputerSetter : MonoBehaviour
{
    [Header("Settings")]
    public ObjectiveComputerData data;

    private bool _hasSet = false;

    private void Start()
    {
        if (ObjectiveSystem.Instance != null)
            ObjectiveSystem.Instance.CurrentObjectiveIndex.OnValueChanged += OnObjectiveIndexChanged;
    }

    private void OnDestroy()
    {
        if (ObjectiveSystem.Instance != null)
            ObjectiveSystem.Instance.CurrentObjectiveIndex.OnValueChanged -= OnObjectiveIndexChanged;
    }

    private void OnObjectiveIndexChanged(int oldIndex, int newIndex)
    {
        TrySet(newIndex);
    }

    private void TrySet(int currentIndex)
    {
        // Only the server rewires computers.
        if (_hasSet || !NetworkManager.Singleton.IsServer) return;
        if (currentIndex != data.NextIndex.x) return;

        var taskComputer = Helper.GetComputerFromTask(data.OriginalIndex.x, data.OriginalIndex.y);
        if (taskComputer == null) { _hasSet = true; return; }

        var newTask = ObjectiveSystem.Instance.ObjectiveList[data.NextIndex.x].tasks[data.NextIndex.y];
        if (newTask is not MinigameTask miniTask) { _hasSet = true; return; }

        taskComputer.associatedTask = miniTask;
        taskComputer.SyncAssociatedTaskClientRpc(data.NextIndex.x, data.NextIndex.y);
        taskComputer.ResetComputerRpc(data.NewHackTime);

        _hasSet = true;
    }
}