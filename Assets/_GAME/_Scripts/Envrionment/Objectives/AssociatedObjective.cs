using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  AssociatedObjective.cs
//
//  Attach this to any world object (door, console, terminal, etc.) that should
//  only be interactable during a specific mission phase.
//
//  How it works:
//    1. On Start, waits until ObjectiveSystem is fully initialised.
//    2. Subscribes to objective/task change events.
//    3. Whenever progress changes, compares the active Objective + Task to
//       the configured ObjectiveIndex / TaskIndex pair.
//    4. Enables or disables every MonoBehaviour in the Interactables list
//       to match. For Computer components the power state is also considered.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Enables or disables a set of MonoBehaviours based on whether the current
/// mission phase matches this component's configured Objective and Task indices.
/// </summary>
public class AssociatedObjective : MonoBehaviour
{
    #region Serialized Fields

    [Header("Target Requirements")]
    [Tooltip("The Objective index (from ObjectiveSystem.ObjectiveList) that must be active.")]
    [SerializeField] private int ObjectiveIndex;

    [Tooltip("The Task index within that Objective that must be the current incomplete task.")]
    [SerializeField] private int TaskIndex;

    [Header("Controlled Components")]
    [Tooltip("MonoBehaviours that will be enabled when the phase matches and disabled otherwise.")]
    [SerializeField] private List<MonoBehaviour> Interactables = new();

    #endregion

    #region Private State

    // Guards against subscribing to ObjectiveSystem events more than once.
    private bool _subscribed;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // ObjectiveSystem may not exist yet on the first frame — wait for it.
        StartCoroutine(WaitForObjectiveSystem());
    }

    private void OnDestroy()
    {
        if (!_subscribed) return;

        // Clean up event subscriptions to avoid null-reference callbacks
        // after this GameObject is destroyed.
        ObjectiveSystem sys = ObjectiveSystem.Instance;
        if (sys != null)
        {
            sys.CurrentObjectiveIndex.OnValueChanged -= OnIndexChanged;
            sys.OnTaskFlagsChangedPublic -= OnTaskFlagsChanged;
        }
    }

    #endregion

    #region Initialisation

    /// <summary>
    /// Spins until the ObjectiveSystem singleton exists and has finished its
    /// network setup, then subscribes and performs the first state sync.
    /// </summary>
    private IEnumerator WaitForObjectiveSystem()
    {
        while (ObjectiveSystem.Instance == null || !ObjectiveSystem.Instance.IsReady)
            yield return null;

        Subscribe();
        RefreshState();
    }

    /// <summary>
    /// Hooks into ObjectiveSystem events so this component reacts to any
    /// mission progress without polling every frame.
    /// Also listens for Computer power-state changes since a powered-off
    /// computer should still be disabled even if the phase matches.
    /// </summary>
    private void Subscribe()
    {
        if (_subscribed) return;

        ObjectiveSystem sys = ObjectiveSystem.Instance;
        sys.CurrentObjectiveIndex.OnValueChanged += OnIndexChanged;
        sys.OnTaskFlagsChangedPublic += OnTaskFlagsChanged;

        // Extra listener for Computer components: the power state is part of
        // the enabled condition, so re-evaluate whenever it changes.
        foreach (MonoBehaviour mono in Interactables)
        {
            if (mono is Computer && mono.TryGetComponent(out ComputerSettings settings))
                settings.IsOn.OnValueChanged += (_, _) => RefreshState();
        }

        _subscribed = true;
    }

    #endregion

    #region State Evaluation

    /// <summary>
    /// Re-evaluates whether interactables should be on or off and applies
    /// the result. Call this whenever any relevant state changes.
    /// </summary>
    private void RefreshState() => SetInteractablesState(IsEnabled());

    /// <summary>
    /// Returns true when the global mission phase matches this component's
    /// configured (ObjectiveIndex, TaskIndex) pair.
    /// </summary>
    private bool IsEnabled()
    {
        ObjectiveSystem sys = ObjectiveSystem.Instance;
        if (sys == null || !sys.IsReady) return false;

        // First check: are we even on the right Objective?
        if (sys.CurrentObjectiveIndex.Value != ObjectiveIndex) return false;

        // Second check: is this the Task the player is currently working on?
        Objective objective = sys.ObjectiveList[ObjectiveIndex];
        return objective.GetCurrentTaskIndex() == TaskIndex;
    }

    /// <summary>
    /// Iterates through all controlled MonoBehaviours and sets enabled = state.
    /// For Computer components, the computer's own power state is AND-ed in so
    /// a turned-off computer can never be enabled by this component alone.
    /// </summary>
    private void SetInteractablesState(bool state)
    {
        foreach (MonoBehaviour mono in Interactables)
        {
            if (mono == null) continue;

            if (mono is Computer && mono.TryGetComponent(out ComputerSettings settings))
            {
                // Computer must both match the phase AND be powered on.
                mono.enabled = state && settings.IsOn.Value;
                continue;
            }

            mono.enabled = state;
        }
    }

    #endregion

    #region Event Callbacks

    // Called when the active Objective index changes (new phase started).
    private void OnIndexChanged(int _, int __) => RefreshState();

    // Called when any task's completion flag changes.
    // Only re-evaluates if the change is relevant to our configured phase.
    private void OnTaskFlagsChanged(int objectiveIndex, int taskIndex)
    {
        if (objectiveIndex == ObjectiveIndex && taskIndex == TaskIndex)
            RefreshState();
    }

    #endregion
}