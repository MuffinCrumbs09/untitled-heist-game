using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  ObjectiveHintBroadcaster.cs
//
//  Host-only system that fires NPC hint lines through SubtitleManager while
//  the player is working on a specific Objective + Task.
//
//  How it works:
//    - Each ObjectiveHintDialogue entry maps an (Objective, Task) pair to a
//      list of hint lines and a speaker name.
//    - When the matching phase is active, a coroutine fires one hint line
//      every _hintInterval seconds using a shuffle-bag so players don't hear
//      the same line twice before the pool resets.
//    - When the phase ends (task complete or objective advances) the coroutine
//      stops and the next matching entry (if any) takes over.
//
//  Only the host drives this system to keep subtitle timing deterministic.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Serializable data for one hint entry — binds an (Objective, Task) pair
/// to a pool of NPC dialogue lines shown on a repeating timer.
/// </summary>
[System.Serializable]
public class ObjectiveHintDialogue
{
    [Tooltip("The Objective index this hint entry applies to.")]
    public int ObjectiveIndex;

    [Tooltip("The Task index within that Objective this hint entry applies to.")]
    public int TaskIndex;

    [Tooltip("NPC name shown in the subtitle bar.")]
    public string NPCName = "NPC";

    [Tooltip("Pool of hint lines. One is chosen at random each interval; " +
             "the pool reshuffles once all lines have been shown.")]
    public List<string> HintLines = new();

    [Tooltip("Seconds to wait after the task becomes active before firing the first hint.")]
    public float InitialDelaySeconds = 5f;
}

/// <summary>
/// NetworkBehaviour that monitors mission progress on the host and broadcasts
/// periodic NPC hint subtitles while specific tasks are in progress.
/// </summary>
public class ObjectiveHintBroadcaster : NetworkBehaviour
{
    #region Serialized Fields

    [Header("Hint Configuration")]
    [SerializeField]
    [Tooltip("Add one entry per (Objective, Task) pair that should have hint dialogue.")]
    private List<ObjectiveHintDialogue> _hintDialogues = new();

    [Header("Timer Settings")]
    [SerializeField]
    [Tooltip("Seconds between consecutive hint broadcasts while a matching task is active.")]
    private float _hintInterval = 30f;

    [SerializeField]
    [Tooltip("How long (seconds) each hint subtitle stays on screen.")]
    private float _subtitleDuration = 4f;

    #endregion

    #region Private State

    // The hint entry whose task is currently active, or null if none match.
    private ObjectiveHintDialogue _activeHint;

    // Shuffle-bag: lines are drawn without replacement; refills when empty.
    private List<string> _bag = new();

    // Reference kept so the coroutine can be stopped when the phase changes.
    private Coroutine _hintCoroutine;

    #endregion

    #region NetworkBehaviour Overrides

    public override void OnNetworkSpawn()
    {
        // Only the host drives hints — clients should not run any of this logic.
        if (!IsHost) return;

        // ObjectiveSystem may not be ready on the frame we spawn, so wait.
        StartCoroutine(WaitAndSubscribe());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsHost) return;

        StopHintCoroutine();

        if (ObjectiveSystem.Instance != null)
        {
            ObjectiveSystem.Instance.OnObjectiveProgressed -= OnObjectiveProgressed;
            ObjectiveSystem.Instance.OnTaskFlagsChangedPublic -= OnTaskChanged;
        }
    }

    #endregion

    #region Initialisation

    /// <summary>
    /// Waits until ObjectiveSystem is fully ready, then subscribes to progress
    /// events and performs an immediate evaluation in case a hint should fire now.
    /// </summary>
    private IEnumerator WaitAndSubscribe()
    {
        yield return new WaitUntil(() =>
            ObjectiveSystem.Instance != null && ObjectiveSystem.Instance.IsReady);

        ObjectiveSystem.Instance.OnObjectiveProgressed += OnObjectiveProgressed;
        ObjectiveSystem.Instance.OnTaskFlagsChangedPublic += OnTaskChanged;

        // Handle the case where we spawn mid-mission and a hint should be active.
        EvaluateCurrentState();
    }

    #endregion

    #region Event Callbacks

    // Fired when the Objective index advances to a new phase.
    private void OnObjectiveProgressed(int newObjectiveIndex, int _)
        => EvaluateCurrentState();

    // Fired when any task's completion flag changes.
    private void OnTaskChanged(int objectiveIndex, int taskIndex)
        => EvaluateCurrentState();

    #endregion

    #region State Evaluation

    /// <summary>
    /// Determines whether the current mission state matches any configured
    /// hint entry. Starts the hint timer if a match is found, stops it if not.
    /// Called whenever Objective or Task progress changes.
    /// </summary>
    private void EvaluateCurrentState()
    {
        if (ObjectiveSystem.Instance == null) return;

        int objectiveIdx = ObjectiveSystem.Instance.CurrentObjectiveIndex.Value;
        int taskIdx = FindFirstIncompleteTask(objectiveIdx);

        ObjectiveHintDialogue matched = FindHintEntry(objectiveIdx, taskIdx);

        // No state change — avoid restarting the timer unnecessarily.
        if (matched == _activeHint) return;

        StopHintCoroutine();
        _activeHint = matched;

        if (_activeHint != null)
        {
            RefillBag(_activeHint);
            _hintCoroutine = StartCoroutine(HintLoop());
        }
    }

    /// <summary>
    /// Scans the current Objective's task list and returns the index of the
    /// first task that is not yet complete, or -1 if all are done.
    /// </summary>
    private int FindFirstIncompleteTask(int objectiveIdx)
    {
        if (ObjectiveSystem.Instance == null) return -1;

        Objective obj = ObjectiveSystem.Instance.GetCurrentObjective();
        if (obj == null) return -1;

        for (int t = 0; t < obj.tasks.Count; t++)
        {
            if (!ObjectiveSystem.Instance.IsTaskCompleted(objectiveIdx, t))
                return t;
        }

        return -1;
    }

    /// <summary>
    /// Looks for an entry in _hintDialogues whose (ObjectiveIndex, TaskIndex)
    /// matches the supplied pair and that has at least one hint line to show.
    /// Returns null if nothing matches.
    /// </summary>
    private ObjectiveHintDialogue FindHintEntry(int objectiveIdx, int taskIdx)
    {
        if (taskIdx < 0) return null;

        foreach (ObjectiveHintDialogue entry in _hintDialogues)
        {
            if (entry.ObjectiveIndex == objectiveIdx &&
                entry.TaskIndex == taskIdx &&
                entry.HintLines != null &&
                entry.HintLines.Count > 0)
            {
                return entry;
            }
        }

        return null;
    }

    #endregion

    #region Hint Coroutine

    /// <summary>
    /// Waits for the initial delay, then broadcasts a hint line on every
    /// _hintInterval until the coroutine is stopped externally.
    /// </summary>
    private IEnumerator HintLoop()
    {
        yield return new WaitForSeconds(_activeHint.InitialDelaySeconds);

        while (true)
        {
            BroadcastNextHint();
            yield return new WaitForSeconds(_hintInterval);
        }
    }

    /// <summary>
    /// Picks a random line from the shuffle-bag and sends it to SubtitleManager.
    /// Refills the bag first if it has been exhausted.
    /// </summary>
    private void BroadcastNextHint()
    {
        if (_activeHint == null || SubtitleManager.Instance == null) return;

        if (_bag.Count == 0)
            RefillBag(_activeHint);

        // Draw randomly and remove the picked line so it won't repeat until the bag refills.
        int pick = Random.Range(0, _bag.Count);
        string line = _bag[pick];
        _bag.RemoveAt(pick);

        SubtitleManager.Instance.ShowNPCSubtitle(_activeHint.NPCName, line, _subtitleDuration);
    }

    /// <summary>Stops the active hint coroutine if one is running.</summary>
    private void StopHintCoroutine()
    {
        if (_hintCoroutine == null) return;
        StopCoroutine(_hintCoroutine);
        _hintCoroutine = null;
    }

    #endregion

    #region Shuffle Bag

    /// <summary>
    /// Copies all hint lines from the given entry into the draw bag.
    /// The bag acts as a "shuffle without replacement" pool — every line
    /// is shown once before any can repeat.
    /// </summary>
    private void RefillBag(ObjectiveHintDialogue hint)
    {
        _bag.Clear();

        if (hint?.HintLines == null) return;

        _bag.AddRange(hint.HintLines);
    }

    #endregion
}