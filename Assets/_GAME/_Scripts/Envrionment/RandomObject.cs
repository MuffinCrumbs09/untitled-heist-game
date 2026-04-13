using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class RandomObject : NetworkBehaviour
{
    public NetworkVariable<bool> isSpawned = new(false);

    public override void OnNetworkSpawn()
    {
        isSpawned.OnValueChanged += UpdateState;
        UpdateState(false, isSpawned.Value);
    }

    [Rpc(SendTo.Server)]
    public void ChangeStateRpc(bool state)
    {
        if (isSpawned.Value == state) return;
        isSpawned.Value = state;
    }

    /// <summary>
    /// Called whenever isSpawned changes. Only enables renderers/colliders/behaviours
    /// if the object is both spawned AND the parent room is currently visible.
    /// Disabling always happens unconditionally so hidden rooms stay hidden.
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
    /// Returns true if found and visible, false if hidden or not found.
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
        // No RoomVisibility in hierarchy — assume visible (e.g. always-on rooms)
        return true;
    }

    /// <summary>
    /// Called by RoomVisibility when the room becomes visible.
    /// Only enables this object if it has been spawned by the map manager.
    /// </summary>
    public void OnRoomShown()
    {
        if (isSpawned.Value)
            EnableNonNetworkBehaviours(transform);
    }

    /// <summary>
    /// Called by RoomVisibility when the room becomes hidden.
    /// Always disables regardless of spawn state.
    /// </summary>
    public void OnRoomHidden()
    {
        DisableNonNetworkBehaviours(transform);
    }

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