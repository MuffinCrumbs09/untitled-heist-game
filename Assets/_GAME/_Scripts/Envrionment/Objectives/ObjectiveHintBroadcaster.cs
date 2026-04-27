using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class ObjectiveHintDialogue
{
    [Tooltip("Objective index this hint applies to")]
    public int ObjectiveIndex;

    [Tooltip("Task index this hint applies to")]
    public int TaskIndex;

    [Tooltip("NPC name displayed in subtitles")]
    public string NPCName = "NPC";

    [Tooltip("Hint lines — one will be picked at random, reshuffled when exhausted")]
    public List<string> HintLines = new();
}

public class ObjectiveHintBroadcaster : NetworkBehaviour
{
    #region Serialized Fields

    [Header("Hint Configuration")]
    [SerializeField]
    [Tooltip("One entry per objective/task pair you want hints for")]
    private List<ObjectiveHintDialogue> _hintDialogues = new();

    [Header("Timer Settings")]
    [SerializeField]
    [Tooltip("Seconds between hint broadcasts")]
    private float _hintInterval = 30f;

    [SerializeField]
    [Tooltip("Initial delay before the first hint fires after a matching task becomes active")]
    private float _initialDelay = 5f;

    [SerializeField]
    [Tooltip("Subtitle display duration passed to SubtitleManager")]
    private float _subtitleDuration = 4f;

    #endregion

    #region Private Fields

    // The hint entry currently active (null = none matched)
    private ObjectiveHintDialogue _activeHint;

    // Remaining lines in the current shuffle-bag
    private List<string> _bag = new();

    private Coroutine _hintCoroutine;

    #endregion

    #region NetworkBehaviour

    public override void OnNetworkSpawn()
    {
        // Only the host drives hints — clients do nothing
        if (!IsHost) return;

        // Wait until ObjectiveSystem signals readiness before subscribing
        StartCoroutine(WaitAndSubscribe());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsHost) return;

        StopHintCoroutine();

        if (ObjectiveSystem.Instance != null)
        {
            ObjectiveSystem.Instance.OnObjectiveProgressed  -= OnObjectiveProgressed;
            ObjectiveSystem.Instance.OnTaskFlagsChangedPublic -= OnTaskChanged;
        }
    }

    #endregion

    #region Initialisation

    private IEnumerator WaitAndSubscribe()
    {
        // Spin until ObjectiveSystem is ready (spawned + lists populated)
        yield return new WaitUntil(() =>
            ObjectiveSystem.Instance != null && ObjectiveSystem.Instance.IsReady);

        ObjectiveSystem.Instance.OnObjectiveProgressed   += OnObjectiveProgressed;
        ObjectiveSystem.Instance.OnTaskFlagsChangedPublic  += OnTaskChanged;

        // Evaluate immediately in case a hint should fire right now
        EvaluateCurrentState();
    }

    #endregion

    #region Objective / Task Callbacks

    private void OnObjectiveProgressed(int newObjectiveIndex, int _)
        => EvaluateCurrentState();

    private void OnTaskChanged(int objectiveIndex, int taskIndex)
        => EvaluateCurrentState();

    #endregion

    #region State Evaluation

    /// <summary>
    /// Checks whether the current objective + active (incomplete) task matches
    /// any configured hint entry, and starts or stops the hint timer accordingly.
    /// </summary>
    private void EvaluateCurrentState()
    {
        if (ObjectiveSystem.Instance == null) return;

        int objectiveIdx = ObjectiveSystem.Instance.CurrentObjectiveIndex.Value;
        int taskIdx      = FindFirstIncompleteTask(objectiveIdx);

        ObjectiveHintDialogue matched = FindHintEntry(objectiveIdx, taskIdx);

        if (matched == _activeHint) return; // No change — do nothing

        StopHintCoroutine();
        _activeHint = matched;

        if (_activeHint != null)
        {
            RefillBag(_activeHint);
            _hintCoroutine = StartCoroutine(HintLoop());
        }
    }

    /// <summary>Returns the first task index that is NOT yet completed, or -1.</summary>
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

        return -1; // All tasks done
    }

    /// <summary>Finds a hint entry matching the given objective + task pair.</summary>
    private ObjectiveHintDialogue FindHintEntry(int objectiveIdx, int taskIdx)
    {
        if (taskIdx < 0) return null;

        foreach (ObjectiveHintDialogue entry in _hintDialogues)
        {
            if (entry.ObjectiveIndex == objectiveIdx &&
                entry.TaskIndex      == taskIdx       &&
                entry.HintLines      != null          &&
                entry.HintLines.Count > 0)
            {
                return entry;
            }
        }

        return null;
    }

    #endregion

    #region Hint Coroutine

    private IEnumerator HintLoop()
    {
        yield return new WaitForSeconds(_initialDelay);

        while (true)
        {
            BroadcastNextHint();
            yield return new WaitForSeconds(_hintInterval);
        }
    }

    private void BroadcastNextHint()
    {
        if (_activeHint == null || SubtitleManager.Instance == null) return;

        if (_bag.Count == 0)
            RefillBag(_activeHint);

        int pick  = Random.Range(0, _bag.Count);
        string line = _bag[pick];
        _bag.RemoveAt(pick);

        SubtitleManager.Instance.ShowNPCSubtitle(_activeHint.NPCName, line, _subtitleDuration);
    }

    private void StopHintCoroutine()
    {
        if (_hintCoroutine == null) return;
        StopCoroutine(_hintCoroutine);
        _hintCoroutine = null;
    }

    #endregion

    #region Shuffle Bag

    /// <summary>
    /// Copies all hint lines into the bag. The bag is drawn without replacement;
    /// when empty it is refilled here before the next pick.
    /// </summary>
    private void RefillBag(ObjectiveHintDialogue hint)
    {
        _bag.Clear();

        if (hint?.HintLines == null) return;

        _bag.AddRange(hint.HintLines);
    }

    #endregion
}