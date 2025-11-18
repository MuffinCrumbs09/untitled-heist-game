using System.Collections.Generic;
using UnityEngine;

public class AssociatedObjective : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int ObjectiveIndex;
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

        bool isEnabled = ObjectiveSystem.Instance.CurrentObjectiveIndex == ObjectiveIndex;

        if (isEnabled != wasEnabled)
        {
            SetInteractablesState(isEnabled);
            wasEnabled = isEnabled;
        }
    }

    private void SetInteractablesState(bool state)
    {
        foreach (MonoBehaviour mono in Interactables)
        {
            if (mono != null)
            {
                mono.enabled = state;
            }
        }
    }
}
