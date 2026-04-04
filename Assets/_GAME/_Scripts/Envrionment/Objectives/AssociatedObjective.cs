using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Enables/disables interactables based on the current objective and task state.
/// Now reads from ObjectiveSystem's NetworkVariables so it reacts correctly on
/// ALL clients, not just the server.
/// </summary>
public class AssociatedObjective : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int ObjectiveIndex;
    [SerializeField] private int TaskIndex;

    [Header("Interactables")]
    [Tooltip("MonoBehaviour scripts (Computer, Door, Keypad, Loot, etc.) that should only be active for this objective.")]
    [SerializeField] private List<MonoBehaviour> Interactables = new();

    private bool _wasEnabled;

    private void Start()
    {
        // Subscribe to Computer.IsOn changes so turning a PC off/on re-evaluates state.
        foreach (MonoBehaviour mono in Interactables)
        {
            if (mono is Computer && mono.TryGetComponent(out ComputerSettings settings))
                settings.IsOn.OnValueChanged += (_, _) => RefreshState();
        }

        // Subscribe to objective index changes so we react immediately when the index ticks.
        if (ObjectiveSystem.Instance != null)
        {
            ObjectiveSystem.Instance.CurrentObjectiveIndex.OnValueChanged += OnObjectiveIndexChanged;
        }

        _wasEnabled = IsEnabled();
        SetInteractablesState(_wasEnabled);
    }

    private void OnDestroy()
    {
        if (ObjectiveSystem.Instance != null)
            ObjectiveSystem.Instance.CurrentObjectiveIndex.OnValueChanged -= OnObjectiveIndexChanged;
    }

    private void OnObjectiveIndexChanged(int oldIndex, int newIndex)
    {
        RefreshState();
    }

    // Keep the Update as a lightweight fallback for task-completion changes
    // (NetworkList<bool> doesn't expose a per-element callback, so we poll lightly).
    private void Update()
    {
        if (ObjectiveSystem.Instance == null) return;

        bool enabled = IsEnabled();
        if (enabled != _wasEnabled)
        {
            SetInteractablesState(enabled);
            _wasEnabled = enabled;
        }
    }

    private void RefreshState()
    {
        bool enabled = IsEnabled();
        SetInteractablesState(enabled);
        _wasEnabled = enabled;
    }

    private bool IsEnabled()
    {
        if (ObjectiveSystem.Instance == null) return false;

        // Check objective index via NetworkVariable (replicated to all clients).
        bool objectiveMatch = ObjectiveSystem.Instance.CurrentObjectiveIndex.Value == ObjectiveIndex;
        if (!objectiveMatch) return false;

        // If TaskIndex > 0, the previous task must be complete before this one activates.
        bool taskMatch = true;
        if (TaskIndex > 0)
        {
            // Read from the replicated completion list.
            taskMatch = ObjectiveSystem.Instance.IsTaskCompleted(ObjectiveIndex, TaskIndex - 1);
        }

        return taskMatch;
    }

    private void SetInteractablesState(bool state)
    {
        foreach (MonoBehaviour mono in Interactables)
        {
            if (mono == null) continue;

            if (mono is Computer && mono.TryGetComponent(out ComputerSettings settings))
            {
                mono.enabled = state && settings.IsOn.Value;
                continue;
            }

            mono.enabled = state;
        }
    }
}