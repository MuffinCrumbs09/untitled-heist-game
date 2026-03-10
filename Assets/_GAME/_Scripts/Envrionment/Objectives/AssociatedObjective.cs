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
        if(TaskIndex > 0)
            taskMatch = ObjectiveSystem.Instance.GetCurObjective().tasks[TaskIndex - 1].isCompleted;
        
        return objectiveMatch && taskMatch;
    }

    private void SetInteractablesState(bool state)
    {
        foreach (MonoBehaviour mono in Interactables)
        {
            if (mono != null)
            {
                if (mono is Computer computer)
                {
                    if (mono.GetComponent<ComputerSettings>().IsOn.Value)
                        mono.enabled = state;
                    
                    continue;
                }
                else
                    mono.enabled = state;
            }
        }
    }
}
