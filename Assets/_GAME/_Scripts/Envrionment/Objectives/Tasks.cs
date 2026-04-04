using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  Base Class
// ─────────────────────────────────────────────────────────────────────────────

[System.Serializable]
public abstract class Task
{
    [Header("Settings")]
    public string taskName;

    /// <summary>
    /// Authoritative completion flag. Written only via ObjectiveSystem.CompleteTask().
    /// Clients should read completion state through ObjectiveSystem.IsTaskCompleted().
    /// </summary>
    public bool isCompleted;

    /// <summary>
    /// Called each frame by the server. Polled tasks should check their condition
    /// here and call objectiveSystem.CompleteTask(objectiveIndex, taskIndex) when met.
    /// Event-driven tasks can leave this empty and rely on an explicit CompleteTask() call.
    /// </summary>
    public abstract void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex);
}

// ─────────────────────────────────────────────────────────────────────────────
//  Custom Task
//  Completion is triggered externally by any world script (door, drill, etc.).
// ─────────────────────────────────────────────────────────────────────────────

public class CustomTask : Task
{
    /// <summary>
    /// Call from any server-side script to mark this task complete.
    /// </summary>
    public void CompleteTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (!isCompleted)
            objectiveSystem.CompleteTask(objectiveIndex, taskIndex);
    }

    public override void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        // Event-driven — nothing to poll.
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Timer Task
//  Completes automatically after a set duration (server time).
// ─────────────────────────────────────────────────────────────────────────────

public class TimerTask : Task
{
    [Header("Timer Settings")]
    public float timerDuration;

    private float _timer;

    public override void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (isCompleted) return;

        _timer += Time.deltaTime;

        if (_timer >= timerDuration)
            objectiveSystem.CompleteTask(objectiveIndex, taskIndex);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Location Task
//  Completes when any player enters one of the target areas.
//  Can also be completed explicitly via CompleteTask() from a trigger zone.
// ─────────────────────────────────────────────────────────────────────────────

public class LocationTask : Task
{
    [Header("Location Settings")]
    public List<Transform> possibleAreas = new();

    /// <summary>
    /// Optional explicit trigger from a collider/zone script (server-side).
    /// </summary>
    public void CompleteTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (!isCompleted)
            objectiveSystem.CompleteTask(objectiveIndex, taskIndex);
    }

    public override void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (isCompleted) return;

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            foreach (Transform area in possibleAreas)
            {
                if (Vector3.Distance(player.transform.position, area.position) <= 2.1f)
                {
                    objectiveSystem.CompleteTask(objectiveIndex, taskIndex);
                    return;
                }
            }
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Loot Task
//  Completes when the team's payout reaches a percentage of the max payout.
// ─────────────────────────────────────────────────────────────────────────────

public class LootTask : Task
{
    [Header("Loot Settings")]
    [Range(1, 100)]
    public int maxPayoutPercent;

    public override void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (isCompleted) return;

        int targetPayout = (NetStore.Instance.MaxPayout.Value * maxPayoutPercent) / 100;

        if (NetStore.Instance.Payout.Value >= targetPayout)
            objectiveSystem.CompleteTask(objectiveIndex, taskIndex);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Minigame Task
//  Completion is triggered by a Computer/HackingMinigame when the player
//  finishes the hack. Polling is intentionally empty.
// ─────────────────────────────────────────────────────────────────────────────

public class MinigameTask : Task
{
    [Header("Minigame Settings")]
    public string RoomType;
    public bool setComputer = true;

    [Header("Random Computer Settings")]
    public bool isRandomComputer = false;
    public Vector2 MinMax;

    /// <summary>
    /// Called by Computer/HackingMinigame on the server when the minigame finishes.
    /// </summary>
    public void CompleteTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        if (!isCompleted)
            objectiveSystem.CompleteTask(objectiveIndex, taskIndex);
    }

    public override void UpdateTask(ObjectiveSystem objectiveSystem, int objectiveIndex, int taskIndex)
    {
        // Event-driven — nothing to poll.
    }
}