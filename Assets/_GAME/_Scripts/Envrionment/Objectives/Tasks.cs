using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  Tasks.cs
//
//  Defines every mission task type used by the ObjectiveSystem.
//
//  How it works:
//    - Each Objective holds a list of Tasks.
//    - ObjectiveSystem calls UpdateTask() on every incomplete task each frame
//      (server-side only).
//    - When a task's completion condition is met, it calls
//      objectiveSystem.CompleteTask(), which sets the shared NetworkList flag
//      so all clients stay in sync.
//
//  To add a new task type, create a new class that inherits from Task and
//  implement UpdateTask(). Polling logic goes there; for event-driven tasks
//  expose a CompleteTask() helper instead and leave UpdateTask() empty.
// ─────────────────────────────────────────────────────────────────────────────


// ─────────────────────────────────────────────────────────────────────────────
//  Base Class
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Abstract base for all mission tasks. Subclasses define how and when
/// a task decides it is done.
/// </summary>
[System.Serializable]
public abstract class Task
{
    #region Inspector Fields

    [Header("Settings")]
    [Tooltip("Display name shown in the mission UI and editor.")]
    public string taskName;

    /// <summary>
    /// True once ObjectiveSystem.CompleteTask() has been called for this task.
    /// Do not set this directly — always go through ObjectiveSystem so the
    /// NetworkList stays in sync across all clients.
    /// </summary>
    [Tooltip("Set automatically by ObjectiveSystem. Do not edit by hand.")]
    public bool isCompleted;

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Called every frame by the server while this task is incomplete.
    /// Polled tasks check their win condition here and call
    /// objectiveSystem.CompleteTask() when it is satisfied.
    /// Event-driven tasks can leave this empty and call CompleteTask() from
    /// whatever world script fires the relevant event.
    /// </summary>
    /// <param name="objectiveSystem">The singleton ObjectiveSystem.</param>
    /// <param name="objectiveIndex">Index of the parent Objective in ObjectiveList.</param>
    /// <param name="taskIndex">Index of this Task within that Objective.</param>
    public abstract void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex);

    #endregion
}


// ─────────────────────────────────────────────────────────────────────────────
//  CustomTask
//  Use when completion is triggered by an arbitrary world script
//  (door sensor, drill finishing, etc.).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A task with no built-in completion logic. Another server-side script is
/// responsible for calling CompleteTask() at the right moment.
/// </summary>
public class CustomTask : Task
{
    #region Public API

    /// <summary>
    /// Call from any server-side script to mark this task complete.
    /// Guards against double-completion automatically.
    /// </summary>
    public void CompleteTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (!isCompleted)
            objectiveSystem.CompleteTask(objectiveIndex, taskIndex);
    }

    #endregion

    #region Task Update

    /// <summary>
    /// Nothing to poll — completion is event-driven from an external script.
    /// </summary>
    public override void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex) { }

    #endregion
}


// ─────────────────────────────────────────────────────────────────────────────
//  TimerTask
//  Automatically completes after a configurable number of seconds.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Completes itself once the server has ticked for <see cref="timerDuration"/>
/// seconds since the task became active.
/// </summary>
public class TimerTask : Task
{
    #region Inspector Fields

    [Header("Timer Settings")]
    [Tooltip("How many seconds must pass (server time) before this task auto-completes.")]
    public float timerDuration;

    #endregion

    #region Private State

    // Accumulated elapsed server time since this task started being updated.
    private float _timer;

    #endregion

    #region Task Update

    /// <summary>
    /// Ticks the countdown on the server only. Completes the task once
    /// the elapsed time exceeds timerDuration.
    /// </summary>
    public override void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (isCompleted) return;
        if (!objectiveSystem.IsServer) return;

        _timer += Time.deltaTime;

        if (_timer >= timerDuration)
            objectiveSystem.CompleteTask(objectiveIndex, taskIndex);
    }

    #endregion
}


// ─────────────────────────────────────────────────────────────────────────────
//  LocationTask
//  Completes when any player gets within range of one of the target transforms,
//  or when an external trigger zone calls CompleteTask() directly.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Completes when a player enters within 4.1 units of any transform in
/// <see cref="possibleAreas"/>. Can also be completed from a trigger zone.
/// </summary>
public class LocationTask : Task
{
    #region Inspector Fields

    [Header("Location Settings")]
    [Tooltip("One or more world positions a player must reach to complete this task.")]
    public List<Transform> possibleAreas = new();

    #endregion

    #region Public API

    /// <summary>
    /// Allows a trigger-zone collider or similar world script to complete this
    /// task explicitly instead of relying on the distance poll.
    /// </summary>
    public void CompleteTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (!isCompleted)
            objectiveSystem.RequestCompleteTaskServerRpc(objectiveIndex, taskIndex);
    }

    #endregion

    #region Task Update

    /// <summary>
    /// Each frame, checks whether any player is within 4.1 units of any target
    /// area. Calls RequestCompleteTaskServerRpc as soon as one is found.
    /// </summary>
    public override void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (isCompleted) return;

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            foreach (Transform area in possibleAreas)
            {
                if (Vector3.Distance(player.transform.position, area.position) <= 4.1f)
                {
                    objectiveSystem.RequestCompleteTaskServerRpc(objectiveIndex, taskIndex);
                    return;
                }
            }
        }
    }

    #endregion
}


// ─────────────────────────────────────────────────────────────────────────────
//  LootTask
//  Completes when the team's payout reaches a target percentage of the max.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Completes once the team's running payout (NetStore.Payout) reaches
/// a configurable percentage of the heist's maximum possible payout.
/// </summary>
public class LootTask : Task
{
    #region Inspector Fields

    [Header("Loot Settings")]
    [Range(1, 100)]
    [Tooltip("What percentage of the maximum payout the team must collect to complete this task (1–100).")]
    public int maxPayoutPercent;

    #endregion

    #region Task Update

    /// <summary>
    /// Server-only poll. Converts maxPayoutPercent into an absolute payout
    /// value and completes the task once the team's running total meets it.
    /// </summary>
    public override void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (isCompleted) return;
        if (!objectiveSystem.IsServer) return;

        int targetPayout = (NetStore.Instance.MaxPayout.Value * maxPayoutPercent) / 100;

        if (NetStore.Instance.Payout.Value >= targetPayout)
            objectiveSystem.CompleteTask(objectiveIndex, taskIndex);
    }

    #endregion
}


// ─────────────────────────────────────────────────────────────────────────────
//  MinigameTask
//  Completion is fired by a Computer / HackingMinigame when the player
//  successfully finishes the hacking sequence.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A task whose completion is triggered by a computer hacking minigame.
/// The Computer script calls CompleteTask() once the player wins the sequence.
/// Supports both fixed and randomly-assigned computers.
/// </summary>
public class MinigameTask : Task
{
    #region Inspector Fields

    [Header("Minigame Settings")]
    [Tooltip("The room type tag used to locate the computer that hosts this task's minigame.")]
    public string RoomType;

    [Tooltip("If true, the ObjectiveSystem will assign a computer to this task on startup.")]
    public bool setComputer = true;

    [Header("Random Computer Settings")]
    [Tooltip("If true, the computer for this task is chosen at random from available terminals.")]
    public bool isRandomComputer = false;

    [Tooltip("Min (X) and Max (Y) values used when randomly selecting a computer index.")]
    public Vector2 MinMax;

    #endregion

    #region Public API

    /// <summary>
    /// Called by the Computer script on the server when the hacking minigame
    /// finishes successfully.
    /// </summary>
    public void CompleteTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (!isCompleted)
            objectiveSystem.CompleteTask(objectiveIndex, taskIndex);
    }

    #endregion

    #region Task Update

    /// <summary>
    /// Nothing to poll — the Computer fires completion via the event-driven
    /// CompleteTask() call above.
    /// </summary>
    public override void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex) { }

    #endregion
}