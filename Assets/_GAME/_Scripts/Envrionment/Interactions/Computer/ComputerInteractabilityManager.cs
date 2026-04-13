using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ComputerInteractabilityManager : NetworkBehaviour
{
    [System.Serializable]
    public class ComputerEntry
    {
        [Tooltip("The computer to control")]
        public Computer computer;

        [Tooltip("Objective index this computer belongs to")]
        public int objectiveIndex;

        [Tooltip("Task index within that objective")]
        public int taskIndex;
    }

    [Header("Entries")]
    public List<ComputerEntry> entries = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (ObjectiveSystem.Instance != null)
        {
            ObjectiveSystem.Instance.CurrentObjectiveIndex.OnValueChanged += OnObjectiveChanged;
            ObjectiveSystem.Instance.OnTaskFlagsChangedPublic += OnTaskCompleted;
            StartCoroutine(ApplyOnNextFrame());
        }
        else
        {
            Debug.LogWarning("[ComputerInteractabilityManager] ObjectiveSystem not found on spawn.");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (ObjectiveSystem.Instance != null)
        {
            ObjectiveSystem.Instance.CurrentObjectiveIndex.OnValueChanged -= OnObjectiveChanged;
            ObjectiveSystem.Instance.OnTaskFlagsChangedPublic -= OnTaskCompleted;
        }
    }

    private IEnumerator ApplyOnNextFrame()
    {
        yield return null;
        ApplyInteractability(ObjectiveSystem.Instance.CurrentObjectiveIndex.Value);
    }

    private void OnObjectiveChanged(int oldIndex, int newIndex)
    {
        ApplyInteractability(newIndex);
    }

    private void OnTaskCompleted(int objectiveIndex, int taskIndex)
    {
        ApplyInteractability(ObjectiveSystem.Instance.CurrentObjectiveIndex.Value);
    }

    private void ApplyInteractability(int currentObjectiveIndex)
    {
        if (ObjectiveSystem.Instance == null) return;

        Objective currentObjective = ObjectiveSystem.Instance.GetCurObjective();
        int currentTaskIndex = currentObjective.GetCurrentTaskIndex();

        foreach (var entry in entries)
        {
            if (entry.computer == null) continue;

            // Computer is off in inspector (ComputerSettings.IsOn) — skip entirely
            ComputerSettings settings = entry.computer.GetComponent<ComputerSettings>();
            if (settings != null && !settings.IsOn.Value) continue;

            bool isCorrectObjective = entry.objectiveIndex == currentObjectiveIndex;
            bool isCorrectTask = entry.taskIndex == currentTaskIndex;
            bool isTaskIncomplete = !ObjectiveSystem.Instance.IsTaskCompleted(entry.objectiveIndex, entry.taskIndex);

            entry.computer.Interactable.Value =
                isCorrectObjective && isCorrectTask && isTaskIncomplete;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].computer == null)
                Debug.LogWarning($"[ComputerInteractabilityManager] Entry {i} has no computer assigned.");
        }
    }
#endif
}