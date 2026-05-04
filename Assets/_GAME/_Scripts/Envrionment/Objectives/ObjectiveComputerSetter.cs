using UnityEngine;
using Unity.Netcode;

// ─────────────────────────────────────────────────────────────────────────────
//  ObjectiveComputerSetter.cs
//
//  Watches the active Objective index and, when the configured phase begins,
//  replaces a Computer's MinigameTask with a new one.
//
//  Why this exists:
//    Some heist designs reuse the same physical computer terminal across
//    multiple mission phases but assign it a different hacking task each time.
//    This component handles that switchover automatically on the server so
//    designers only need to configure a ScriptableObject.
//
//  Server-only: all Computer state changes must originate on the server.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Listens for a specific Objective to become active, then rewires a Computer
/// in the scene to use a new MinigameTask and (optionally) a new hack duration.
/// Only executes on the server. Runs the rewire exactly once per session.
/// </summary>
public class ObjectiveComputerSetter : MonoBehaviour
{
    #region Inspector Fields

    [Header("Rewire Configuration")]
    [Tooltip("ScriptableObject that defines which computer to rewire, which new task to assign, and an optional new hack time.")]
    public ObjectiveComputerData data;

    #endregion

    #region Private State

    // Ensures the rewire only fires once even if the objective callback fires multiple times.
    private bool _hasSet = false;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // Subscribe to objective changes so we know when the target phase starts.
        if (ObjectiveSystem.Instance != null)
            ObjectiveSystem.Instance.CurrentObjectiveIndex.OnValueChanged += OnObjectiveIndexChanged;
    }

    private void OnDestroy()
    {
        if (ObjectiveSystem.Instance != null)
            ObjectiveSystem.Instance.CurrentObjectiveIndex.OnValueChanged -= OnObjectiveIndexChanged;
    }

    #endregion

    #region Event Callbacks

    /// <summary>
    /// Fired by ObjectiveSystem whenever the active Objective index changes.
    /// Passes the new index to TrySet for validation.
    /// </summary>
    private void OnObjectiveIndexChanged(int oldIndex, int newIndex)
    {
        TrySet(newIndex);
    }

    #endregion

    #region Rewire Logic

    /// <summary>
    /// Attempts the computer rewire when the active Objective matches
    /// the configured NextIndex.x. Skips silently if already done, not the
    /// server, or if the target objects cannot be found.
    /// </summary>
    /// <param name="currentIndex">The Objective index that just became active.</param>
    private void TrySet(int currentIndex)
    {
        if (_hasSet || !NetworkManager.Singleton.IsServer) return;

        // Only act when the Objective that needs the rewired computer goes live.
        if (currentIndex != data.NextIndex.x) return;

        // Locate the Computer object that currently owns the original task.
        var taskComputer = Helper.GetComputerFromTask(data.OriginalIndex.x, data.OriginalIndex.y);
        if (taskComputer == null)
        {
            _hasSet = true; // Nothing to rewire — mark done to avoid repeated attempts.
            return;
        }

        // Get the new Task and confirm it is a MinigameTask (computers only support those).
        var newTask = ObjectiveSystem.Instance.ObjectiveList[data.NextIndex.x].tasks[data.NextIndex.y];
        if (newTask is not MinigameTask miniTask)
        {
            _hasSet = true;
            return;
        }

        // Apply the new task locally, sync it to all clients, then reset the minigame.
        taskComputer.associatedTask = miniTask;
        taskComputer.SyncAssociatedTaskClientRpc(data.NextIndex.x, data.NextIndex.y);
        taskComputer.ResetComputerRpc(data.NewHackTime);

        _hasSet = true;
    }

    #endregion
}