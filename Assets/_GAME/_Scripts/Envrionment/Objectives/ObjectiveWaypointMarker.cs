using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
//  ObjectiveWaypointMarker.cs
//
//  Shows a UI waypoint marker on a world object after the player has been
//  working on the relevant task for longer than WaypointDelaySeconds.
//
//  Design intent: don't spoil the objective immediately — give players time
//  to find the location themselves, then surface the marker as a fallback.
//
//  Rules for showing the marker:
//    1. The task's Objective must be active.
//    2. All earlier tasks in that Objective must already be complete.
//    3. This specific task must not yet be complete.
//    4. The room containing this object must be visible (if RoomVisibility exists).
//    5. WaypointDelaySeconds must have elapsed since all of the above became true.
//
//  The server owns the _markerVisible NetworkVariable; clients just reflect it.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Manages a UI waypoint marker that appears above a world object once a
/// configured delay has passed, helping players locate an objective target.
/// </summary>
public class ObjectiveWaypointMarker : NetworkBehaviour
{
    #region Inspector

    [Header("Objective Reference")]
    [Tooltip("X = Objective index, Y = Task index that this marker belongs to.")]
    public Vector2Int ObjectiveIndex;

    [Header("UI")]
    [Tooltip("The Image component used as the on-screen waypoint indicator.")]
    public Image WaypointMarker;

    [Header("Settings")]
    [Tooltip("Seconds the player must be on this task before the marker appears. " +
             "Gives players time to find the location themselves first.")]
    public float WaypointDelaySeconds = 120f;

    #endregion

    #region Networked State

    // Server writes; clients read. Drives the marker UI across all peers.
    private readonly NetworkVariable<bool> _markerVisible = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    #endregion

    #region Private State

    private ObjectiveSystem _system;
    private RoomVisibility _room;

    // Guards against double-subscribing to ObjectiveSystem events.
    private bool _subscribed;

    // The Time.time value when the delay timer started. -1 means not started.
    private float _activationTime = -1f;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        _room = GetComponent<RoomVisibility>();
        SetMarkerUI(false); // Hidden by default until the system evaluates.
    }

    public override void OnNetworkSpawn()
    {
        // All peers react to visibility changes so the UI stays in sync.
        _markerVisible.OnValueChanged += OnMarkerChanged;

        if (_room?.IsVisible != null)
            _room.IsVisible.OnValueChanged += OnRoomVisibilityChanged;

        // Reflect whatever the server already decided (important for late joiners).
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

        // Retry initialisation each frame until ObjectiveSystem is ready.
        if (!_subscribed)
            TryInitialize();

        // Only tick the delay timer once a valid activation time has been set.
        if (_activationTime >= 0f)
            Evaluate();
    }

    #endregion

    #region Setup

    /// <summary>
    /// Attempts to connect to ObjectiveSystem. Silently returns if it isn't
    /// ready yet — Update() will retry next frame.
    /// </summary>
    private void TryInitialize()
    {
        _system = ObjectiveSystem.Instance;

        if (_system == null || !_system.IsReady)
            return;

        Subscribe();
        Evaluate();
    }

    #endregion

    #region Event Subscription

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

    #region Core Evaluation Logic

    /// <summary>
    /// Single authoritative method that decides whether the waypoint marker
    /// should be visible. Called whenever any relevant state changes or each
    /// frame while the delay timer is counting down.
    /// </summary>
    private void Evaluate()
    {
        if (_system == null || !_system.IsReady)
        {
            SetMarker(false);
            return;
        }

        // Hide and reset if the room isn't accessible to the player yet.
        if (!IsRoomVisible())
        {
            ResetActivation();
            SetMarker(false);
            return;
        }

        bool isCurrentObjective = _system.CurrentObjectiveIndex.Value == ObjectiveIndex.x;
        bool isCompleted = _system.IsTaskCompleted(ObjectiveIndex.x, ObjectiveIndex.y);

        // Hide if we're on the wrong objective, task is done, or earlier tasks aren't done.
        if (!isCurrentObjective || isCompleted || !PreviousTasksDone())
        {
            ResetActivation();
            SetMarker(false);
            return;
        }

        // Start the delay timer the first time all conditions are satisfied.
        if (_activationTime < 0f)
            _activationTime = Time.time;

        // Show the marker only after the delay has elapsed.
        bool shouldShow = Time.time - _activationTime >= WaypointDelaySeconds;
        SetMarker(shouldShow);
    }

    /// <summary>
    /// Returns true if the room system says this area is currently accessible,
    /// or true unconditionally if there is no RoomVisibility component.
    /// </summary>
    private bool IsRoomVisible()
    {
        if (_room == null) return true;
        if (_room.IsVisible == null) return false; // Room exists but not initialised — treat as hidden.
        return _room.IsVisible.Value;
    }

    /// <summary>Resets the delay timer so it restarts if conditions become valid again.</summary>
    private void ResetActivation()
    {
        _activationTime = -1f;
    }

    /// <summary>
    /// Returns true if every task before this one in the same Objective is
    /// already complete — ensures the marker doesn't show out of order.
    /// </summary>
    private bool PreviousTasksDone()
    {
        for (int i = 0; i < ObjectiveIndex.y; i++)
        {
            if (!_system.IsTaskCompleted(ObjectiveIndex.x, i))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Sets the _markerVisible NetworkVariable, which propagates the change
    /// to all clients automatically. No-ops if the value isn't changing.
    /// </summary>
    private void SetMarker(bool value)
    {
        if (_markerVisible.Value != value)
            _markerVisible.Value = value;
    }

    #endregion

    #region Event Callbacks

    // Re-evaluate only when the changed task belongs to our Objective.
    private void OnTaskChanged(int objIdx, int taskIdx)
    {
        if (objIdx != ObjectiveIndex.x) return;
        Evaluate();
    }

    // Re-evaluate whenever the active Objective changes.
    private void OnObjectiveChanged(int newIdx, int _)
    {
        Evaluate();
    }

    // Server-side: re-evaluate when room visibility changes (e.g. a locked room opens).
    private void OnRoomVisibilityChanged(bool _, bool __)
    {
        if (!IsServer) return;
        Evaluate();
    }

    #endregion

    #region Client UI

    // Called on all clients when the server changes _markerVisible.
    private void OnMarkerChanged(bool _, bool current)
    {
        SetMarkerUI(current);
    }

    /// <summary>Directly toggles the marker Image component on or off.</summary>
    private void SetMarkerUI(bool visible)
    {
        if (WaypointMarker != null)
            WaypointMarker.enabled = visible;
    }

    #endregion
}