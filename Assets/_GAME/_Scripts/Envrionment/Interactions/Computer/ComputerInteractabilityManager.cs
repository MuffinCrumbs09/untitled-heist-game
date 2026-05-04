using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-side manager that automatically enables or disables computers 
/// based on the current active mission objective and task.
/// </summary>
public class ComputerInteractabilityManager : NetworkBehaviour
{
    [System.Serializable]
    public class ComputerEntry
    {
        public Computer computer;
        public int objectiveIndex;
        public int taskIndex;
    }

    [Header("Registry")]
    [Tooltip("List of all computers and their corresponding mission logic indices.")]
    public List<ComputerEntry> entries = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (ObjectiveSystem.Instance != null)
        {
            ObjectiveSystem.Instance.CurrentObjectiveIndex.OnValueChanged += OnObjectiveChanged;
            ObjectiveSystem.Instance.OnTaskFlagsChangedPublic += OnTaskCompleted;
            StartCoroutine(InitialSetupRoutine());
        }
    }

    private IEnumerator InitialSetupRoutine()
    {
        yield return null; // Wait for NetworkVariables to settle
        ApplyInteractability(ObjectiveSystem.Instance.CurrentObjectiveIndex.Value);
    }

    private void OnObjectiveChanged(int oldIndex, int newIndex) => ApplyInteractability(newIndex);
    private void OnTaskCompleted(int objIdx, int taskIdx) => ApplyInteractability(ObjectiveSystem.Instance.CurrentObjectiveIndex.Value);

    /// <summary>
    /// Iterates through registry and toggles 'Interactable' state based on mission progress.
    /// </summary>
    private void ApplyInteractability(int currentObjectiveIndex)
    {
        if (ObjectiveSystem.Instance == null) return;

        int currentTaskIndex = ObjectiveSystem.Instance.GetCurObjective().GetCurrentTaskIndex();

        foreach (var entry in entries)
        {
            if (entry.computer == null) continue;

            // Only allow interaction if computer is on
            ComputerSettings settings = entry.computer.GetComponent<ComputerSettings>();
            if (settings != null && !settings.IsOn.Value) continue;

            bool isMatch = entry.objectiveIndex == currentObjectiveIndex && entry.taskIndex == currentTaskIndex;
            bool isIncomplete = !ObjectiveSystem.Instance.IsTaskCompleted(entry.objectiveIndex, entry.taskIndex);

            entry.computer.Interactable.Value = isMatch && isIncomplete;
        }
    }
}