using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  Objective.cs
//
//  Represents a single high-level mission goal (e.g. "Reach the vault").
//  Each Objective owns an ordered list of Tasks that must all be completed
//  before the Objective is considered done and the next one begins.
//
//  Lifecycle:
//    1. ObjectiveSystem.Update() calls UpdateObjective() on the active Objective.
//    2. On the first call (server only) BeginObjective() fires — playing NPC
//       dialogue if configured.
//    3. Each incomplete Task is updated in order.
//    4. Once IsCompleted() returns true, ObjectiveSystem advances to the next
//       Objective automatically.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A serializable data structure representing one mission phase.
/// Contains metadata, optional intro dialogue, and an ordered Task list.
/// </summary>
[System.Serializable]
public class Objective
{
    #region Inspector Fields

    [Header("Metadata")]
    [Tooltip("Human-readable name shown in the mission UI.")]
    public string objectiveName;

    [SerializeReference]
    [Tooltip("Ordered list of Tasks that must all be completed to finish this Objective.")]
    public List<Task> tasks;

    [Header("Intro Dialogue")]
    [Tooltip("The NPC name displayed in the subtitle bar when this Objective begins.")]
    public string speakerName;

    [TextArea]
    [Tooltip("The line of dialogue spoken by the NPC the moment this Objective becomes active. Leave blank to skip dialogue.")]
    public string speech;

    #endregion

    #region Private State

    // Prevents BeginObjective() from firing more than once per Objective instance.
    private bool _hasStarted = false;

    #endregion

    #region Public API

    /// <summary>
    /// Returns true when every Task in the list is marked complete.
    /// ObjectiveSystem polls this each frame to know when to advance.
    /// </summary>
    public bool IsCompleted()
    {
        foreach (var task in tasks)
        {
            if (!task.isCompleted)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Called every frame by ObjectiveSystem while this Objective is active.
    /// Handles one-time startup on the server and drives per-task polling.
    /// </summary>
    /// <param name="objectiveSystem">The singleton manager driving this call.</param>
    public void UpdateObjective(ObjectiveSystem objectiveSystem)
    {
        // BeginObjective must only run once and only on the server.
        if (!_hasStarted && NetworkManager.Singleton.IsServer)
            BeginObjective();

        int objectiveIndex = objectiveSystem.ObjectiveList.IndexOf(this);

        // Only poll tasks that still need to be finished.
        for (int i = 0; i < tasks.Count; i++)
        {
            if (!tasks[i].isCompleted)
                tasks[i].UpdateTask(objectiveSystem, objectiveIndex, i);
        }
    }

    /// <summary>
    /// Returns the index of the first Task that has not yet been completed,
    /// or -1 if all tasks are done. Used by AssociatedObjective and other
    /// components that need to know which step the player is currently on.
    /// </summary>
    public int GetCurrentTaskIndex()
    {
        for (int i = 0; i < tasks.Count; i++)
            if (!tasks[i].isCompleted)
                return i;

        return -1;
    }

    #endregion

    #region Internal Logic

    /// <summary>
    /// Runs once on the server when the Objective first becomes active.
    /// Triggers NPC subtitle dialogue if both speakerName and speech are set.
    /// </summary>
    private void BeginObjective()
    {
        _hasStarted = true;

        if (string.IsNullOrEmpty(speakerName) || string.IsNullOrEmpty(speech)) return;

        SubtitleManager.Instance.ShowNPCSubtitle(speakerName, speech, 3.75f);
    }

    #endregion
}