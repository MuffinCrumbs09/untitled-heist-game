using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Stats;

public class ObjectiveSystem : NetworkBehaviour
{
    #region Fields and Properties
    public static ObjectiveSystem Instance;

    public List<Objective> ObjectiveList = new();

    public NetworkVariable<int> CurrentObjectiveIndex = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkList<bool> _taskCompletionFlags;

    public event System.Action<int, int> OnTaskFlagsChangedPublic;

    public event System.Action<int, int> OnObjectiveProgressed;

    public bool IsReady { get; private set; }

    private int[] _objectiveOffsets;

    private Stats.PlayerStats _stats;
    private bool _heistEnded;
    #endregion

    #region Unity Lifecycle
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
        BuildOffsetTable();

        if (IsServer)
        {
            foreach (var objective in ObjectiveList)
                foreach (var _ in objective.tasks)
                    _taskCompletionFlags.Add(false);

            _stats = SaveManager.Instance.LoadGame();
        }

        CurrentObjectiveIndex.OnValueChanged += OnObjectiveIndexChanged;

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

    private void Update()
    {
        if (_heistEnded) return;

        int idx = CurrentObjectiveIndex.Value;

        if (idx >= ObjectiveList.Count)
        {
            if (IsServer)
                EndHeist();
            return;
        }

        Objective current = ObjectiveList[idx];
        current.UpdateObjective(this);

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

    public void CompleteTask(int objectiveIndex, int taskIndex)
    {
        if (!IsServer) return;

        int flatIndex = GetFlatIndex(objectiveIndex, taskIndex);
        if (!IsFlatIndexValid(flatIndex)) return;
        if (_taskCompletionFlags[flatIndex]) return;

        _taskCompletionFlags[flatIndex] = true;

        ObjectiveList[objectiveIndex].tasks[taskIndex].isCompleted = true;

        OnTaskFlagsChangedPublic?.Invoke(objectiveIndex, taskIndex);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestCompleteTaskServerRpc(int objectiveIndex, int taskIndex)
    {
        CompleteTask(objectiveIndex, taskIndex);
    }

    public bool IsTaskCompleted(int objectiveIndex, int taskIndex)
    {
        int flatIndex = GetFlatIndex(objectiveIndex, taskIndex);
        return IsFlatIndexValid(flatIndex) && _taskCompletionFlags[flatIndex];
    }

    #endregion

    #region Helpers

    public Objective GetCurrentObjective()
    {
        int idx = CurrentObjectiveIndex.Value;
        return idx < ObjectiveList.Count ? ObjectiveList[idx] : null;
    }

    public Objective GetCurObjective() => GetCurrentObjective();

    private int GetFlatIndex(int objectiveIndex, int taskIndex)
    {
        if (_objectiveOffsets == null || (uint)objectiveIndex >= (uint)_objectiveOffsets.Length)
            return -1;
        return _objectiveOffsets[objectiveIndex] + taskIndex;
    }

    private bool IsFlatIndexValid(int flatIndex)
        => flatIndex >= 0 && flatIndex < _taskCompletionFlags.Count;

    #endregion

    #region Callbacks

    private void OnObjectiveIndexChanged(int oldValue, int newValue)
    {
        OnObjectiveProgressed?.Invoke(newValue, 0);
    }

    private void OnTaskFlagsChanged(NetworkListEvent<bool> changeEvent)
    {
        if (changeEvent.Type != NetworkListEvent<bool>.EventType.Value) return;

        int flatIndex = changeEvent.Index;

        for (int o = 0; o < ObjectiveList.Count; o++)
        {
            int offset = _objectiveOffsets[o];
            int count  = ObjectiveList[o].tasks.Count;

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

    private void EndHeist()
    {
        _heistEnded = true;

        _stats.TotalMoneyStole += NetStore.Instance.Payout.Value;
        _stats.TotalKills      += NetPlayerManager.Instance.GetLocalPlayersKills();
        _stats.TotalHeists++;
        SaveManager.Instance.SaveGame(_stats);

        SaveStatsClientRpc(NetStore.Instance.Payout.Value);

        ShutdownClientRpc();
        StartCoroutine(ShutdownAfterDelay());
    }

    [Rpc(SendTo.NotServer)]
    private void SaveStatsClientRpc(int payout)
    {
        var clientStats = SaveManager.Instance.LoadGame();
        clientStats.TotalMoneyStole += payout;
        clientStats.TotalKills      += NetPlayerManager.Instance.GetLocalPlayersKills();
        clientStats.TotalHeists++;
        SaveManager.Instance.SaveGame(clientStats);
    }

    private System.Collections.IEnumerator ShutdownAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        NetworkManager.Singleton.Shutdown();
        Destroy(NetPlayerManager.Instance.gameObject);
        Destroy(NetworkManager.Singleton.gameObject);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    [Rpc(SendTo.NotServer)]
    private void ShutdownClientRpc()
    {
        StartCoroutine(ClientShutdownRoutine());
    }

    private System.Collections.IEnumerator ClientShutdownRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        NetworkManager.Singleton.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    #endregion
}