using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Stats;

/// <summary>
/// Server-authoritative objective system.
/// CurrentObjectiveIndex is a NetworkVariable so all clients stay in sync automatically.
/// Task completion is driven on the server and broadcast via RPC.
/// Stats are saved only by the server (host) at heist end.
/// </summary>
public class ObjectiveSystem : NetworkBehaviour
{
    public static ObjectiveSystem Instance;

    public List<Objective> ObjectiveList = new();

    // Replicated to all clients — source of truth for which objective is active.
    public NetworkVariable<int> CurrentObjectiveIndex = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Replicated flat array of task completion flags.
    // Layout: objective 0 tasks, then objective 1 tasks, etc.
    // Populated once on server spawn; clients read it passively.
    private NetworkList<bool> _taskCompletionFlags;

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
    }

    public override void OnNetworkDespawn()
    {
        CurrentObjectiveIndex.OnValueChanged -= OnObjectiveIndexChanged;
    }

    private void Update()
    {
        // All objective logic runs on the server only.
        if (!IsServer || _heistEnded) return;

        int idx = CurrentObjectiveIndex.Value;

        // All objectives done — end the heist.
        if (idx >= ObjectiveList.Count)
        {
            EndHeist();
            return;
        }

        Objective current = ObjectiveList[idx];
        current.UpdateObjective(this);

        if (current.IsCompleted())
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

    // ──────────────────────────────────────────────
    //  Heist End (Server only)
    // ──────────────────────────────────────────────

    private void EndHeist()
    {
        _heistEnded = true;

        // Save stats on the server (host player) only.
        _stats.TotalMoneyStole += NetStore.Instance.Payout.Value;
        _stats.TotalKills      += NetPlayerManager.Instance.GetLocalPlayersKills();
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
        clientStats.TotalKills      += kills;
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