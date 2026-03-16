using System.Collections.Generic;
using UnityEngine;

public class AssociatedObjective : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int ObjectiveIndex;
    [SerializeField] private int TaskIndex;
    [Header("Interactables")]
    [Tooltip("Add the MonoBehaviour scripts (Computer, Door, Keypad, Loot, etc.) that should only be active for this objective.")]
    [SerializeField] private List<MonoBehaviour> Interactables = new();

    private bool wasEnabled;

    private void Start()
    {
        // Subscribe to IsOn changes for all computers
        foreach (MonoBehaviour mono in Interactables)
        {
            if (mono is Computer && mono.TryGetComponent(out ComputerSettings settings))
                settings.IsOn.OnValueChanged += (_, _) => SetInteractablesState(IsEnabled());
        }

        wasEnabled = ObjectiveSystem.Instance.CurrentObjectiveIndex == ObjectiveIndex;
        SetInteractablesState(wasEnabled);
    }


    private void Update()
    {
        if (ObjectiveSystem.Instance == null) return;

        if (IsEnabled() != wasEnabled)
        {
            SetInteractablesState(IsEnabled());
            wasEnabled = IsEnabled();
        }
    }

    private bool IsEnabled()
    {
        bool objectiveMatch = ObjectiveSystem.Instance.CurrentObjectiveIndex == ObjectiveIndex;
        bool taskMatch = true;
        if (TaskIndex > 0)
            taskMatch = ObjectiveSystem.Instance.GetCurObjective().tasks[TaskIndex - 1].isCompleted;

        return objectiveMatch && taskMatch;
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
