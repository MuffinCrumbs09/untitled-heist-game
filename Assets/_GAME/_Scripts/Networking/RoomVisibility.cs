using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// The server sets IsVisible; all clients react via the NetworkVariable callback.
/// RandomObject children are excluded from this component's renderer/collider cache —
/// they manage their own visibility and are notified via OnRoomShown/OnRoomHidden.
/// </summary>
public class RoomVisibility : NetworkBehaviour
{
    public NetworkVariable<bool> IsVisible = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Renderer[] _renderers;
    private Collider[] _colliders;

    // RandomObject children that live directly inside this room (not nested in another RoomVisibility)
    private RandomObject[] _randomObjects;

    public override void OnNetworkSpawn()
    {
        CacheComponents();
        IsVisible.OnValueChanged += (_, newVal) => Apply(newVal);
        Apply(IsVisible.Value);
    }

    /// <summary>Marks this room as visible. Must be called on the server only.</summary>
    public void Show()
    {
        IsVisible.Value = true;
    }

    /// <summary>Marks this room as hidden. Must be called on the server only.</summary>
    public void Hide()
    {
        IsVisible.Value = false;
    }

    private void Apply(bool visible)
    {
        foreach (var r in _renderers) r.enabled = visible;
        foreach (var c in _colliders) c.enabled = visible;

        // Notify RandomObject children so they can respect both room visibility and spawn state
        foreach (var ro in _randomObjects)
        {
            if (visible)
                ro.OnRoomShown();
            else
                ro.OnRoomHidden();
        }
    }

    /// <summary>
    /// Gathers Renderer/Collider components in this room's hierarchy,
    /// EXCLUDING subtrees rooted on a RandomObject (they self-manage).
    /// Also collects the direct RandomObject children for visibility callbacks.
    /// </summary>
    private void CacheComponents()
    {
        var renderers = new List<Renderer>();
        var colliders = new List<Collider>();
        var randomObjects = new List<RandomObject>();

        CollectExcludingRandomObjects(transform, renderers, colliders, randomObjects);

        _renderers = renderers.ToArray();
        _colliders = colliders.ToArray();
        _randomObjects = randomObjects.ToArray();
    }

    private void CollectExcludingRandomObjects(
        Transform t,
        List<Renderer> renderers,
        List<Collider> colliders,
        List<RandomObject> randomObjects)
    {
        // If this node (other than the room root itself) has a RandomObject,
        // record it for callbacks but do NOT descend — RandomObject owns that subtree.
        if (t != transform && t.TryGetComponent<RandomObject>(out var ro))
        {
            randomObjects.Add(ro);
            return;
        }

        if (t.TryGetComponent<Renderer>(out var r)) renderers.Add(r);
        colliders.AddRange(t.GetComponents<Collider>());

        foreach (Transform child in t)
            CollectExcludingRandomObjects(child, renderers, colliders, randomObjects);
    }
}