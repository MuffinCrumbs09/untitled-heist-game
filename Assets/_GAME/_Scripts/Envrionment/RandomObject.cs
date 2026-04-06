using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class RandomObject : NetworkBehaviour
{
    public NetworkVariable<bool> isSpawned = new(false);
    #region Inspector Fields

    #endregion

    public override void OnNetworkSpawn()
    {
        isSpawned.OnValueChanged += UpdateState;
        UpdateState(false, isSpawned.Value);
    }

    [Rpc(SendTo.Server)]
    public void ChangeStateRpc(bool state)
    {
        if(isSpawned.Value == state) return;
        isSpawned.Value = state;
    }

    private void UpdateState(bool previous, bool current)
    {
        if (current)
        {
            EnableNonNetworkBehaviours(transform);
        }
        else
        {
            DisableNonNetworkBehaviours(transform);
        }
    }

    private void DisableNonNetworkBehaviours(Transform t)
    {
        foreach (Transform child in t)
        {
            DisableNonNetworkBehaviours(child);
        }

        var behaviours = t.GetComponents<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (!(behaviour is NetworkBehaviour))
            {
                behaviour.enabled = false;
            }
        }

        var renderers = t.GetComponents<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }

        var colliders = t.GetComponents<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private void EnableNonNetworkBehaviours(Transform t)
    {
        foreach (Transform child in t)
        {
            EnableNonNetworkBehaviours(child);
        }

        var behaviours = t.GetComponents<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (!(behaviour is NetworkBehaviour))
            {
                behaviour.enabled = true;
            }
        }

        var renderers = t.GetComponents<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = true;
        }

        var colliders = t.GetComponents<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }
    }
}