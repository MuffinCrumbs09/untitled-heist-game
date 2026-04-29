using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveWaypointMarker : NetworkBehaviour
{
    #region Inspector
    [Header("Objective Reference")]
    public Vector2Int ObjectiveIndex;

    [Header("UI")]
    public Image WaypointMarker;

    [Header("Settings")]
    public float WaypointDelaySeconds = 120f;
    #endregion

    private readonly NetworkVariable<bool> _markerVisible = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private ObjectiveSystem _system;
    private RoomVisibility _room;

    private bool _subscribed;
    private float _activationTime = -1f;

    #region Lifecycle
    private void Awake()
    {
        _room = GetComponent<RoomVisibility>();
        SetMarkerUI(false);
    }

    public override void OnNetworkSpawn()
    {
        _markerVisible.OnValueChanged += OnMarkerChanged;

        if (_room?.IsVisible != null)
            _room.IsVisible.OnValueChanged += OnRoomVisibilityChanged;

        SetMarkerUI(_markerVisible.Value);

        if (IsServer)
            TryInitialize();
    }

    public override void OnNetworkDespawn()
    {
        _markerVisible.OnValueChanged -= OnMarkerChanged;

        if (_room?.IsVisible != null)
            _room.IsVisible.OnValueChanged -= OnRoomVisibilityChanged;

        if (IsServer)
            Unsubscribe();
    }

    private void Update()
    {
        if (!IsServer) return;

        if (!_subscribed)
            TryInitialize();

        // Only used for delay ticking
        if (_activationTime >= 0f)
            Evaluate();
    }
    #endregion

    #region Setup
    private void TryInitialize()
    {
        _system = ObjectiveSystem.Instance;

        if (_system == null || !_system.IsReady)
            return;

        Subscribe();
        Evaluate();
    }
    #endregion

    #region Subscription
    private void Subscribe()
    {
        if (_subscribed) return;

        _system.OnTaskFlagsChangedPublic += OnTaskChanged;
        _system.OnObjectiveProgressed += OnObjectiveChanged;

        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || _system == null) return;

        _system.OnTaskFlagsChangedPublic -= OnTaskChanged;
        _system.OnObjectiveProgressed -= OnObjectiveChanged;

        _subscribed = false;
    }
    #endregion

    #region Core Logic (Single Source of Truth)
    private void Evaluate()
    {
        if (_system == null || !_system.IsReady)
        {
            SetMarker(false);
            return;
        }

        if (!IsRoomVisible())
        {
            ResetActivation();
            SetMarker(false);
            return;
        }

        bool isCurrentObjective = _system.CurrentObjectiveIndex.Value == ObjectiveIndex.x;
        bool isCompleted = _system.IsTaskCompleted(ObjectiveIndex.x, ObjectiveIndex.y);

        if (!isCurrentObjective || isCompleted || !PreviousTasksDone())
        {
            ResetActivation();
            SetMarker(false);
            return;
        }

        // Start delay timer if not started
        if (_activationTime < 0f)
            _activationTime = Time.time;

        bool shouldShow = Time.time - _activationTime >= WaypointDelaySeconds;
        SetMarker(shouldShow);
    }

    private bool IsRoomVisible()
    {
        // No room system → always visible
        if (_room == null)
            return true;

        // Room exists but visibility not initialized → treat as hidden (safer)
        if (_room.IsVisible == null)
            return false;

        return _room.IsVisible.Value;
    }

    private void ResetActivation()
    {
        _activationTime = -1f;
    }

    private bool PreviousTasksDone()
    {
        for (int i = 0; i < ObjectiveIndex.y; i++)
        {
            if (!_system.IsTaskCompleted(ObjectiveIndex.x, i))
                return false;
        }
        return true;
    }

    private void SetMarker(bool value)
    {
        if (_markerVisible.Value != value)
            _markerVisible.Value = value;
    }
    #endregion

    #region Events → just trigger Evaluate()
    private void OnTaskChanged(int objIdx, int taskIdx)
    {
        if (objIdx != ObjectiveIndex.x) return;
        Evaluate();
    }

    private void OnObjectiveChanged(int newIdx, int _)
    {
        Evaluate();
    }

    private void OnRoomVisibilityChanged(bool _, bool __)
    {
        if (!IsServer) return;
        Evaluate();
    }
    #endregion

    #region Client UI
    private void OnMarkerChanged(bool _, bool current)
    {
        SetMarkerUI(current);
    }

    private void SetMarkerUI(bool visible)
    {
        if (WaypointMarker != null)
            WaypointMarker.enabled = visible;
    }
    #endregion
}