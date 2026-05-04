using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Stats;

// ─────────────────────────────────────────────────────────────────────────────
//  ObjectiveSystem.cs
//
//  Central networked manager for all mission progress.
//
//  Architecture overview:
//    - Holds the master list of Objectives, each with their Tasks.
//    - Tracks which Objective is currently active via CurrentObjectiveIndex
//      (a NetworkVariable — the server writes it, clients read it).
//    - Tracks individual Task completion via a flat NetworkList<bool>.
//      Tasks are flattened to a 1-D array using a pre-built offset table so
//      look-ups are O(1).
//    - Fires events (OnTaskFlagsChangedPublic, OnObjectiveProgressed) so other
//      components can react without polling.
//    - When all Objectives are complete the server calls EndHeist(), saves
//      stats, and shuts down the network session for all clients.
//
//  Server / Client split:
//    - Only the server writes NetworkVariables and calls CompleteTask().
//    - Clients subscribe to change events and update local task state to match.
//    - Clients may request task completion via RequestCompleteTaskServerRpc().
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Singleton NetworkBehaviour that owns and drives all mission Objectives and
/// their Tasks. Must be present in every heist scene.
/// </summary>
public class ObjectiveSystem : NetworkBehaviour
{
    #region Fields and Properties

    /// <summary>Global singleton reference. Set in Awake, cleared on destroy.</summary>
    public static ObjectiveSystem Instance;

    /// <summary>
    /// Designer-populated list of all Objectives for this heist, in order.
    /// Populated in the Inspector. Do not modify at runtime.
    /// </summary>
    public List<Objective> ObjectiveList = new();

    /// <summary>
    /// Index into ObjectiveList indicating the currently active Objective.
    /// Written by the server; readable by all clients.
    /// </summary>
    public NetworkVariable<int> CurrentObjectiveIndex = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Flat array of completion booleans, one per Task across all Objectives.
    // Index mapping: use _objectiveOffsets[objIdx] + taskIdx.
    private NetworkList<bool> _taskCompletionFlags;

    /// <summary>
    /// Fires on both server and clients when a Task's completion flag changes.
    /// Parameters: (objectiveIndex, taskIndex).
    /// </summary>
    public event System.Action<int, int> OnTaskFlagsChangedPublic;

    /// <summary>
    /// Fires when the active Objective index advances.
    /// Parameters: (newObjectiveIndex, 0 — reserved for future use).
    /// </summary>
    public event System.Action<int, int> OnObjectiveProgressed;

    /// <summary>
    /// True once OnNetworkSpawn has finished setup. Components should wait on
    /// this flag before querying the system (see AssociatedObjective).
    /// </summary>
    public bool IsReady { get; private set; }

    // Pre-computed start index (into _taskCompletionFlags) for each Objective.
    private int[] _objectiveOffsets;

    private Stats.PlayerStats _stats;

    // Prevents EndHeist() from running more than once.
    private bool _heistEnded;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Enforce singleton — destroy any duplicate that spawns.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        // NetworkList must be created in Awake, before OnNetworkSpawn.
        _taskCompletionFlags = new NetworkList<bool>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    }

    /// <summary>
    /// Called by Netcode once this object is fully spawned on the network.
    /// Server populates the task flag list; clients subscribe to changes.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // Build the offset table so flat-index look-ups are O(1).
        BuildOffsetTable();

        if (IsServer)
        {
            // Populate one flag per Task across all Objectives, all starting false.
            foreach (var objective in ObjectiveList)
                foreach (var _ in objective.tasks)
                    _taskCompletionFlags.Add(false);

            _stats = SaveManager.Instance.LoadGame();
        }

        CurrentObjectiveIndex.OnValueChanged += OnObjectiveIndexChanged;

        // Clients sync local task state by listening to the NetworkList directly.
        if (!IsServer)
            _taskCompletionFlags.OnListChanged += OnTaskFlagsChanged;

        IsReady = true;
    }

    public override void OnNetworkDespawn()
    {
        IsReady = false;
        CurrentObjectiveIndex.OnValueChanged -= OnObjectiveIndexChanged;

        if (!IsServer)
            _taskCompletionFlags.OnListChanged -= OnTaskFlagsChanged;
    }

    /// <summary>
    /// Drives the active Objective each frame on all peers.
    /// Only the server actually writes state; clients just read.
    /// </summary>
    private void Update()
    {
        if (_heistEnded) return;

        int idx = CurrentObjectiveIndex.Value;

        // If we've gone past the last Objective, the heist is won.
        if (idx >= ObjectiveList.Count)
        {
            if (IsServer)
                EndHeist();
            return;
        }

        Objective current = ObjectiveList[idx];
        current.UpdateObjective(this);

        // Server advances the index when all tasks in the current Objective are done.
        if (IsServer && current.IsCompleted())
        {
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment,
                $"Objective '{current.objectiveName}' completed.", this);
#endif
            CurrentObjectiveIndex.Value++;
        }
    }

    #endregion

    #region Offset Table

    /// <summary>
    /// Pre-computes the starting flat index for each Objective so individual
    /// task look-ups don't need to sum counts at runtime.
    /// Called once in OnNetworkSpawn before any task reads/writes.
    /// </summary>
    private void BuildOffsetTable()
    {
        _objectiveOffsets = new int[ObjectiveList.Count];
        int offset = 0;
        for (int i = 0; i < ObjectiveList.Count; i++)
        {
            _objectiveOffsets[i] = offset;
            offset += ObjectiveList[i].tasks.Count;
        }
    }

    #endregion

    #region Task Completion API

    /// <summary>
    /// Marks a specific task as complete. Server-only.
    /// Updates the NetworkList (syncing all clients) and fires the public event.
    /// Safe to call multiple times — silently ignored if already complete.
    /// </summary>
    /// <param name="objectiveIndex">Index of the parent Objective.</param>
    /// <param name="taskIndex">Index of the Task within that Objective.</param>
    public void CompleteTask(int objectiveIndex, int taskIndex)
    {
        if (!IsServer) return;

        int flatIndex = GetFlatIndex(objectiveIndex, taskIndex);
        if (!IsFlatIndexValid(flatIndex)) return;
        if (_taskCompletionFlags[flatIndex]) return; // Already done.

        _taskCompletionFlags[flatIndex] = true;

        // Keep the local Task object's flag in sync (the server reads this directly).
        ObjectiveList[objectiveIndex].tasks[taskIndex].isCompleted = true;

        OnTaskFlagsChangedPublic?.Invoke(objectiveIndex, taskIndex);
    }

    /// <summary>
    /// RPC allowing any client (or server) to request that a task be completed.
    /// The server validates and calls CompleteTask() if appropriate.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestCompleteTaskServerRpc(int objectiveIndex, int taskIndex)
    {
        CompleteTask(objectiveIndex, taskIndex);
    }

    /// <summary>
    /// Returns true if the specified task has been completed.
    /// Safe to call from any peer.
    /// </summary>
    public bool IsTaskCompleted(int objectiveIndex, int taskIndex)
    {
        int flatIndex = GetFlatIndex(objectiveIndex, taskIndex);
        return IsFlatIndexValid(flatIndex) && _taskCompletionFlags[flatIndex];
    }

    #endregion

    #region Helpers

    /// <summary>Returns the currently active Objective, or null if all are done.</summary>
    public Objective GetCurrentObjective()
    {
        int idx = CurrentObjectiveIndex.Value;
        return idx < ObjectiveList.Count ? ObjectiveList[idx] : null;
    }

    /// <summary>Alias for GetCurrentObjective() — kept for backwards compatibility.</summary>
    public Objective GetCurObjective() => GetCurrentObjective();

    /// <summary>
    /// Converts (objectiveIndex, taskIndex) to a position in the flat
    /// _taskCompletionFlags list. Returns -1 for invalid indices.
    /// </summary>
    private int GetFlatIndex(int objectiveIndex, int taskIndex)
    {
        if (_objectiveOffsets == null || (uint)objectiveIndex >= (uint)_objectiveOffsets.Length)
            return -1;
        return _objectiveOffsets[objectiveIndex] + taskIndex;
    }

    /// <summary>Returns true if flatIndex is within the bounds of the flag list.</summary>
    private bool IsFlatIndexValid(int flatIndex)
        => flatIndex >= 0 && flatIndex < _taskCompletionFlags.Count;

    #endregion

    #region Network Callbacks

    /// <summary>
    /// Called on all peers when CurrentObjectiveIndex changes.
    /// Broadcasts OnObjectiveProgressed so listeners can react immediately.
    /// </summary>
    private void OnObjectiveIndexChanged(int oldValue, int newValue)
    {
        OnObjectiveProgressed?.Invoke(newValue, 0);
    }

    /// <summary>
    /// Client-side handler for changes to the _taskCompletionFlags NetworkList.
    /// Translates a flat index change back into (objectiveIndex, taskIndex) and
    /// syncs the local Task object, then fires the public event.
    /// </summary>
    private void OnTaskFlagsChanged(NetworkListEvent<bool> changeEvent)
    {
        // We only care about value updates, not Add/Remove housekeeping events.
        if (changeEvent.Type != NetworkListEvent<bool>.EventType.Value) return;

        int flatIndex = changeEvent.Index;

        // Walk the offset table to find which Objective and Task this index belongs to.
        for (int o = 0; o < ObjectiveList.Count; o++)
        {
            int offset = _objectiveOffsets[o];
            int count = ObjectiveList[o].tasks.Count;

            if (flatIndex >= offset && flatIndex < offset + count)
            {
                int t = flatIndex - offset;
                ObjectiveList[o].tasks[t].isCompleted = changeEvent.Value;
                OnTaskFlagsChangedPublic?.Invoke(o, t);
                return;
            }
        }
    }

    #endregion

    #region Heist End

    /// <summary>
    /// Called by the server when all Objectives are complete.
    /// Saves stats for the server player, then instructs all clients to save
    /// their own stats and return to the main menu.
    /// </summary>
    private void EndHeist()
    {
        _heistEnded = true;

        // Save server-side stats.
        _stats.TotalMoneyStole += NetStore.Instance.Payout.Value;
        _stats.TotalKills += NetPlayerManager.Instance.GetLocalPlayersKills();
        _stats.TotalHeists++;
        SaveManager.Instance.SaveGame(_stats);

        // Tell each client to save their own stats before we shut down.
        SaveStatsClientRpc(NetStore.Instance.Payout.Value);

        ShutdownClientRpc();
        StartCoroutine(ShutdownAfterDelay());
    }

    /// <summary>
    /// Received by all non-server clients. Each client saves its own stats
    /// using the payout value broadcast from the server.
    /// </summary>
    [Rpc(SendTo.NotServer)]
    private void SaveStatsClientRpc(int payout)
    {
        var clientStats = SaveManager.Instance.LoadGame();
        clientStats.TotalMoneyStole += payout;
        clientStats.TotalKills += NetPlayerManager.Instance.GetLocalPlayersKills();
        clientStats.TotalHeists++;
        SaveManager.Instance.SaveGame(clientStats);
    }

    /// <summary>
    /// Server-side shutdown: waits briefly so in-flight RPCs can land,
    /// then tears down Netcode and returns to the main menu (scene 0).
    /// </summary>
    private System.Collections.IEnumerator ShutdownAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        NetworkManager.Singleton.Shutdown();
        Destroy(NetPlayerManager.Instance.gameObject);
        Destroy(NetworkManager.Singleton.gameObject);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    /// <summary>
    /// Tells all non-server clients to shut down and return to the main menu.
    /// </summary>
    [Rpc(SendTo.NotServer)]
    private void ShutdownClientRpc()
    {
        StartCoroutine(ClientShutdownRoutine());
    }

    /// <summary>
    /// Client-side shutdown: brief delay to match server timing, then
    /// tears down Netcode and loads the main menu.
    /// </summary>
    private System.Collections.IEnumerator ClientShutdownRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        NetworkManager.Singleton.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    #endregion

    #region Debug (Editor Only)

#if UNITY_EDITOR
    /// <summary>
    /// Right-click context menu helper. Completes the next incomplete task in
    /// the current Objective so you can test objective progression in-editor
    /// without playing through the level normally.
    /// </summary>
    [ContextMenu("Complete Current Task")]
    private void Debug_CompleteCurrentTask()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[ObjectiveSystem] Must be the server to complete tasks.");
            return;
        }

        if (!IsReady)
        {
            Debug.LogWarning("[ObjectiveSystem] ObjectiveSystem is not ready yet.");
            return;
        }

        int objIdx = CurrentObjectiveIndex.Value;
        if (objIdx >= ObjectiveList.Count)
        {
            Debug.LogWarning("[ObjectiveSystem] No active objective — heist may already be complete.");
            return;
        }

        Objective current = ObjectiveList[objIdx];

        for (int t = 0; t < current.tasks.Count; t++)
        {
            if (!IsTaskCompleted(objIdx, t))
            {
                CompleteTask(objIdx, t);
                Debug.Log($"[ObjectiveSystem] Completed task {t} ('{current.tasks[t]}') " +
                          $"of objective {objIdx} ('{current.objectiveName}').");
                return;
            }
        }

        Debug.LogWarning($"[ObjectiveSystem] All tasks in objective {objIdx} " +
                         $"('{current.objectiveName}') are already completed.");
    }
#endif

    #endregion
}