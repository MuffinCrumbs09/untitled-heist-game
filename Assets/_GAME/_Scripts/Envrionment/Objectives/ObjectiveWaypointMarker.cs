using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveWaypointMarker : NetworkBehaviour
{
    #region Inspector Fields

    [Header("Objective Reference")]
    [Tooltip("X = Objective index, Y = Task index (both zero-based)")]
    public Vector2Int ObjectiveIndex;

    [Header("UI")]
    public Image WaypointMarker;

    [Header("Settings")]
    [Tooltip("Seconds after this task becomes active before the waypoint hint appears. Set 0 to show immediately.")]
    public float WaypointDelaySeconds = 120f;

    #endregion

    #region State

    private RoomVisibility _room;
    private bool _subscribed;
    private float _taskActivatedTime = -1f;
    private bool _markerVisible;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (WaypointMarker != null)
            WaypointMarker.enabled = false;
    }

    private void Start()
    {
        _room = GetComponent<RoomVisibility>();
    }

    private void Update()
    {
        if (_taskActivatedTime >= 0f && !_markerVisible)
        {
            if (Time.time - _taskActivatedTime >= WaypointDelaySeconds)
                SetMarker(true);
        }
    }

    public override void OnNetworkDespawn() => Unsubscribe();
    public override void OnDestroy() => Unsubscribe();

    #endregion

    #region Public API

    public void Setup()
    {
        bool roomOk = _room == null || (_room.IsVisible != null && _room.IsVisible.Value);
        if (!roomOk) return;

        Subscribe();
        RefreshMarker();
    }

    #endregion

    #region Event Subscription

    private void Subscribe()
    {
        if (_subscribed) return;
        var sys = ObjectiveSystem.Instance;
        if (sys == null) return;

        sys.OnTaskFlagsChangedPublic += OnTaskCompleted;
        sys.OnObjectiveProgressed += OnObjectiveProgressed;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        var sys = ObjectiveSystem.Instance;
        if (sys != null)
        {
            sys.OnTaskFlagsChangedPublic -= OnTaskCompleted;
            sys.OnObjectiveProgressed -= OnObjectiveProgressed;
        }
        _subscribed = false;
    }

    #endregion

    #region Marker Logic

    private void RefreshMarker()
    {
        var sys = ObjectiveSystem.Instance;
        if (sys == null || !sys.IsReady) return;

        int curObjective = sys.CurrentObjectiveIndex.Value;
        bool isMyObjective = curObjective == ObjectiveIndex.x;
        bool isCompleted = sys.IsTaskCompleted(ObjectiveIndex.x, ObjectiveIndex.y);

        if (!isMyObjective || isCompleted)
        {
            _taskActivatedTime = -1f;
            SetMarker(false);
            return;
        }

        if (!ArePreviousTasksCompleted(sys))
        {
            _taskActivatedTime = -1f;
            SetMarker(false);
            return;
        }

        if (_taskActivatedTime < 0f)
            _taskActivatedTime = Time.time;

        SetMarker((Time.time - _taskActivatedTime) >= WaypointDelaySeconds);
    }

    private bool ArePreviousTasksCompleted(ObjectiveSystem sys)
    {
        for (int t = 0; t < ObjectiveIndex.y; t++)
        {
            if (!sys.IsTaskCompleted(ObjectiveIndex.x, t))
                return false;
        }
        return true;
    }

    private void SetMarker(bool visible)
    {
        _markerVisible = visible;
        if (WaypointMarker != null)
            WaypointMarker.enabled = visible;
    }

    private void OnTaskCompleted(int objectiveIdx, int taskIdx)
    {
        if (objectiveIdx != ObjectiveIndex.x) return;

        if (taskIdx == ObjectiveIndex.y)
        {
            _taskActivatedTime = -1f;
            SetMarker(false);
        }
        else if (taskIdx < ObjectiveIndex.y)
        {
            RefreshMarker();
        }
    }

    private void OnObjectiveProgressed(int newObjectiveIndex, int _)
    {
        if (newObjectiveIndex == ObjectiveIndex.x)
            _taskActivatedTime = -1f;

        RefreshMarker();
    }

    #endregion
}