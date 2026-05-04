using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Networked component that controls whether a randomly-spawned map object is visible and active.
/// The map manager decides whether each RandomObject should exist on a given run by calling
/// ChangeStateRpc. This component then shows or hides the object (and all its children)
/// by toggling renderers, colliders, and non-network MonoBehaviours.
/// Also integrates with RoomVisibility so objects in hidden rooms stay hidden even if spawned.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class RandomObject : NetworkBehaviour
{
    // Whether this object has been selected to exist in the current map run.
    // Set by the server via ChangeStateRpc. Replicated to all clients.
    public NetworkVariable<bool> isSpawned = new(false);

    /// <summary>
    /// Subscribes to isSpawned changes and applies the current state immediately on spawn.
    /// Handles late-joining clients who missed the initial state change.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        isSpawned.OnValueChanged += UpdateState;

        // Apply the current value right away in case this client joined after the state was set.
        UpdateState(false, isSpawned.Value);
    }

    /// <summary>
    /// Server RPC that sets whether this object should be spawned/active.
    /// Only applies the change if the new state differs from the current one.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void ChangeStateRpc(bool state)
    {
        if (isSpawned.Value == state) return;
        isSpawned.Value = state;
    }

    /// <summary>
    /// Called whenever isSpawned changes value.
    /// Enables or disables all renderers, colliders, and behaviours only if the object is both spawned
    /// AND inside a currently visible room. 
    /// </summary>
    private void UpdateState(bool previous, bool current)
    {
        if (current && IsRoomVisible())
            EnableNonNetworkBehaviours(transform);
        else
            DisableNonNetworkBehaviours(transform);
    }

    /// <summary>
    /// Walks up the hierarchy to find the nearest RoomVisibility component.
    /// Returns true if the room is currently visible, false if hidden or no RoomVisibility exists.
    /// If no RoomVisibility is found, assumes the object should be treated as always visible.
    /// </summary>
    private bool IsRoomVisible()
    {
        Transform t = transform.parent;
        while (t != null)
        {
            if (t.TryGetComponent(out RoomVisibility vis))
                return vis.IsVisible.Value;
            t = t.parent;
        }

        // No RoomVisibility found in the hierarchy — treat as always visible.
        return true;
    }

    /// <summary>
    /// Called by RoomVisibility when the parent room becomes visible.
    /// Only enables this object if it has already been spawned by the map manager.
    /// Prevents unspawned objects from appearing when a room is revealed.
    /// </summary>
    public void OnRoomShown()
    {
        if (isSpawned.Value)
            EnableNonNetworkBehaviours(transform);
    }

    /// <summary>
    /// Called by RoomVisibility when the parent room becomes hidden.
    /// Always disables this object regardless of its spawn state.
    /// </summary>
    public void OnRoomHidden()
    {
        DisableNonNetworkBehaviours(transform);
    }

    /// <summary>
    /// Recursively disables all renderers, colliders, and non-NetworkBehaviour MonoBehaviours
    /// on this Transform and all of its children.
    /// NetworkBehaviours are deliberately skipped to preserve network state.
    /// </summary>
    private void DisableNonNetworkBehaviours(Transform t)
    {
        foreach (Transform child in t)
            DisableNonNetworkBehaviours(child);

        foreach (var behaviour in t.GetComponents<MonoBehaviour>())
            if (behaviour is not NetworkBehaviour)
                behaviour.enabled = false;

        foreach (var r in t.GetComponents<Renderer>())
            r.enabled = false;

        foreach (var c in t.GetComponents<Collider>())
            c.enabled = false;
    }

    /// <summary>
    /// Recursively enables all renderers, colliders, and non-NetworkBehaviour MonoBehaviours
    /// on this Transform and all of its children.
    /// </summary>
    private void EnableNonNetworkBehaviours(Transform t)
    {
        foreach (Transform child in t)
            EnableNonNetworkBehaviours(child);

        foreach (var behaviour in t.GetComponents<MonoBehaviour>())
            if (behaviour is not NetworkBehaviour)
                behaviour.enabled = true;

        foreach (var r in t.GetComponents<Renderer>())
            r.enabled = true;

        foreach (var c in t.GetComponents<Collider>())
            c.enabled = true;
    }
}