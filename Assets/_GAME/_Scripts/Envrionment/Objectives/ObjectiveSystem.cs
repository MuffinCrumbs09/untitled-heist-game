using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Stats;


public class ObjectiveSystem : NetworkBehaviour
{
    public static ObjectiveSystem Instance;

    public List<Objective> ObjectiveList = new();

    public NetworkVariable<int> CurrentObjectiveIndex = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Replicated flat array of task completion flags.
    // Layout: objective 0 tasks, then objective 1 tasks, etc.
    // Populated once on server spawn; clients read it passively.
    private NetworkList<bool> _taskCompletionFlags;

    public event System.Action<int, int> OnTaskFlagsChangedPublic;

    /// <summary>
    /// True once OnNetworkSpawn has finished building _objectiveOffsets and
    /// syncing NetworkVariables. AssociatedObjective waits on this before
    /// subscribing, so clients never read state before it's valid.
    /// </summary>
    public bool IsReady { get; private set; }

    // Cached offsets so we can map (objectiveIndex, taskIndex) → flat index quickly.
    private int[] _objectiveOffsets;

    private Stats.PlayerStats _stats;
    private bool _heistEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _taskCompletionFlags = new NetworkList<bool>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Build the flat task-completion list and offset table.
            _objectiveOffsets = new int[ObjectiveList.Count];
            int offset = 0;
            for (int i = 0; i < ObjectiveList.Count; i++)
            {
                _objectiveOffsets[i] = offset;
                foreach (var _ in ObjectiveList[i].tasks)
                {
                    _taskCompletionFlags.Add(false);
                }
                offset += ObjectiveList[i].tasks.Count;
            }

            _stats = SaveManager.Instance.LoadGame();
        }
        else
        {
            // Clients rebuild the offset table from the list sizes (no server data needed —
            // ObjectiveList is a scene-configured inspector list, identical on all clients).
            _objectiveOffsets = new int[ObjectiveList.Count];
            int offset = 0;
            for (int i = 0; i < ObjectiveList.Count; i++)
            {
                _objectiveOffsets[i] = offset;
                offset += ObjectiveList[i].tasks.Count;
            }
        }

        // Hook so AssociatedObjective and UI can react to index changes.
        CurrentObjectiveIndex.OnValueChanged += OnObjectiveIndexChanged;

        // Clients need to mirror task.isCompleted from the NetworkList so that
        // Objective.IsCompleted() and UpdateTask()'s early-exit guard work correctly.
        // The server already sets task.isCompleted directly in CompleteTask().
        if (!IsServer)
        {
            _taskCompletionFlags.OnListChanged += OnTaskFlagsChanged;
        }

        // Signal that offsets are built and NetworkVariables are live.
        // AssociatedObjective.WaitForObjectiveSystem() gates on this.
        IsReady = true;
    }

    public override void OnNetworkDespawn()
    {
        IsReady = false;
        CurrentObjectiveIndex.OnValueChanged -= OnObjectiveIndexChanged;
        if (!IsServer)
            _taskCompletionFlags.OnListChanged -= OnTaskFlagsChanged;
    }

    private void Update()
    {
        // Heist ended, stop processing
        if (_heistEnded) return;

        int idx = CurrentObjectiveIndex.Value;

        // All objectives done — end the heist (server only).
        if (idx >= ObjectiveList.Count)
        {
            if (IsServer)
                EndHeist();
            return;
        }

        // Run objective updates on all clients so LocationTask and other client-side checks work
        Objective current = ObjectiveList[idx];
        current.UpdateObjective(this);

        // Check if objective is complete (server only, to avoid duplicate completion)
        if (IsServer && current.IsCompleted())
        {
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"Objective '{current.objectiveName}' completed.", this);
#endif
            CurrentObjectiveIndex.Value++;
        }
    }

    // ──────────────────────────────────────────────
    //  Task Completion API (called by task types)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Mark a task as complete. Must be called on the server.
    /// </summary>
    public void CompleteTask(int objectiveIndex, int taskIndex)
    {
        if (!IsServer) return;

        int flatIndex = GetFlatIndex(objectiveIndex, taskIndex);
        if (flatIndex < 0 || flatIndex >= _taskCompletionFlags.Count) return;

        if (_taskCompletionFlags[flatIndex]) return; // already done

        _taskCompletionFlags[flatIndex] = true;

        // Also update the in-memory task object so IsCompleted() works immediately.
        ObjectiveList[objectiveIndex].tasks[taskIndex].isCompleted = true;

        OnTaskFlagsChangedPublic?.Invoke(objectiveIndex, taskIndex);
    }

    /// <summary>
    /// RPC that clients can call to request task completion (e.g., LocationTask).
    /// Server verifies and completes the task via the NetworkList.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestCompleteTaskServerRpc(int objectiveIndex, int taskIndex)
    {
        // Server-side verification and completion
        CompleteTask(objectiveIndex, taskIndex);
    }

    /// <summary>
    /// Returns whether a task is complete (reads from NetworkList, safe on all clients).
    /// </summary>
    public bool IsTaskCompleted(int objectiveIndex, int taskIndex)
    {
        int flatIndex = GetFlatIndex(objectiveIndex, taskIndex);
        if (flatIndex < 0 || flatIndex >= _taskCompletionFlags.Count) return false;
        return _taskCompletionFlags[flatIndex];
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    public Objective GetCurObjective()
    {
        int idx = CurrentObjectiveIndex.Value;
        return idx >= ObjectiveList.Count
            ? ObjectiveList[ObjectiveList.Count - 1]
            : ObjectiveList[idx];
    }

    private int GetFlatIndex(int objectiveIndex, int taskIndex)
    {
        if (_objectiveOffsets == null || objectiveIndex >= _objectiveOffsets.Length) return -1;
        return _objectiveOffsets[objectiveIndex] + taskIndex;
    }

    private void OnObjectiveIndexChanged(int oldValue, int newValue)
    {
        // Clients and server both receive this callback.
        // UI / AssociatedObjective systems react via their own subscriptions to this NetworkVariable.
    }

    /// <summary>
    /// Called on clients when the server updates the NetworkList.
    /// Mirrors the flag into the local task object so IsCompleted() and
    /// the isCompleted early-exit guard in UpdateTask() work correctly.
    /// </summary>
    private void OnTaskFlagsChanged(NetworkListEvent<bool> changeEvent)
    {
        if (changeEvent.Type != NetworkListEvent<bool>.EventType.Value) return;

        int flatIndex = changeEvent.Index;
        bool completed = changeEvent.Value;

        for (int o = 0; o < ObjectiveList.Count; o++)
        {
            int offset = _objectiveOffsets[o];
            int count = ObjectiveList[o].tasks.Count;
            if (flatIndex >= offset && flatIndex < offset + count)
            {
                int t = flatIndex - offset;
                ObjectiveList[o].tasks[t].isCompleted = completed;

                // Notify subscribers (e.g. AssociatedObjective) so they don't need to poll.
                OnTaskFlagsChangedPublic?.Invoke(o, t);
                return;
            }
        }
    }

    // ──────────────────────────────────────────────
    //  Heist End (Server only)
    // ──────────────────────────────────────────────

    private void EndHeist()
    {
        _heistEnded = true;

        // Save stats on the server (host player) only.
        _stats.TotalMoneyStole += NetStore.Instance.Payout.Value;
        _stats.TotalKills += NetPlayerManager.Instance.GetLocalPlayersKills();
        _stats.TotalHeists++;
        SaveManager.Instance.SaveGame(_stats);

        // Tell all clients to save their own stats before we shut down.
        SaveStatsClientRpc(
            NetStore.Instance.Payout.Value,
            NetPlayerManager.Instance.GetLocalPlayersKills()
        );

        StartCoroutine(ShutdownAfterDelay());
    }

    /// <summary>
    /// Sent to every client so they can write their own local stats file.
    /// The host already saved in EndHeist(); IsHost guard prevents a double-save.
    /// </summary>
    [Rpc(SendTo.NotServer)]
    private void SaveStatsClientRpc(int payout, int kills)
    {
        var clientStats = SaveManager.Instance.LoadGame();
        clientStats.TotalMoneyStole += payout;
        clientStats.TotalKills += kills;
        clientStats.TotalHeists++;
        SaveManager.Instance.SaveGame(clientStats);
    }

    private System.Collections.IEnumerator ShutdownAfterDelay()
    {
        // Small delay so the ClientRpc above reaches clients before we kill the network.
        yield return new WaitForSeconds(0.5f);

        NetworkManager.Singleton.Shutdown();
        Destroy(NetPlayerManager.Instance.gameObject);
        Destroy(NetworkManager.Singleton.gameObject);

        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}