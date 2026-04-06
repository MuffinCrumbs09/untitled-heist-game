using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Enables/disables interactables based on the current objective and task state.
/// Fully event-driven — no per-frame polling. Reacts to:
///   • ObjectiveSystem.CurrentObjectiveIndex changes  (NetworkVariable callback)
///   • ObjectiveSystem.OnTaskFlagsChangedPublic       (NetworkList callback forwarded)
///   • Computer.IsOn changes                          (NetworkVariable callback)
///
/// Waits for ObjectiveSystem.IsReady (set at end of OnNetworkSpawn) before
/// subscribing, so clients never read state before NetworkVariables have synced.
/// </summary>
public class AssociatedObjective : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int ObjectiveIndex;
    [SerializeField] private int TaskIndex;

    [Header("Interactables")]
    [Tooltip("MonoBehaviour scripts (Computer, Door, Keypad, Loot, etc.) that should only be active for this objective.")]
    [SerializeField] private List<MonoBehaviour> Interactables = new();

    private bool _subscribed;

    // ─────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────

    private void Start()
    {
        StartCoroutine(WaitForObjectiveSystem());
    }

    /// <summary>
    /// Waits until ObjectiveSystem is fully initialised — meaning OnNetworkSpawn
    /// has run and _objectiveOffsets has been built. On clients this is later than
    /// just Instance != null (which is set in Awake, before network spawn).
    /// </summary>
    private IEnumerator WaitForObjectiveSystem()
    {
        while (ObjectiveSystem.Instance == null || !ObjectiveSystem.Instance.IsReady)
            yield return null;

        Subscribe();
        RefreshState();
    }

    private void Subscribe()
    {
        if (_subscribed) return;

        ObjectiveSystem sys = ObjectiveSystem.Instance;
        sys.CurrentObjectiveIndex.OnValueChanged += OnIndexChanged;
        sys.OnTaskFlagsChangedPublic              += OnTaskFlagsChanged;

        foreach (MonoBehaviour mono in Interactables)
        {
            if (mono is Computer && mono.TryGetComponent(out ComputerSettings settings))
                settings.IsOn.OnValueChanged += (_, _) => RefreshState();
        }

        _subscribed = true;
    }

    private void OnDestroy()
    {
        if (!_subscribed) return;

        ObjectiveSystem sys = ObjectiveSystem.Instance;
        if (sys != null)
        {
            sys.CurrentObjectiveIndex.OnValueChanged -= OnIndexChanged;
            sys.OnTaskFlagsChangedPublic              -= OnTaskFlagsChanged;
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Callbacks
    // ─────────────────────────────────────────────────────────

    private void OnIndexChanged(int _, int __) => RefreshState();

    private void OnTaskFlagsChanged(int objectiveIndex, int taskIndex)
    {
        if (objectiveIndex == ObjectiveIndex && taskIndex == TaskIndex)
            RefreshState();
    }

    // ─────────────────────────────────────────────────────────
    //  State Logic
    // ─────────────────────────────────────────────────────────

    private void RefreshState() => SetInteractablesState(IsEnabled());

    private bool IsEnabled()
    {
        ObjectiveSystem sys = ObjectiveSystem.Instance;
        if (sys == null || !sys.IsReady) return false;

        if (sys.CurrentObjectiveIndex.Value != ObjectiveIndex) return false;

        Objective objective = sys.ObjectiveList[ObjectiveIndex];
        return objective.GetCurrentTaskIndex() == TaskIndex;
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