using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        // Allow the owner (the Client) to control the animation state
        return false;
    }

    /// <summary>
    /// Call this instead of Animator.SetLayerWeight on the owning client.
    /// Syncs the weight to all other clients via the server.
    /// </summary>
    public void SetLayerWeight(int layerIndex, float weight)
    {
        if (IsOwner)
        {
            Animator.SetLayerWeight(layerIndex, weight);
            SetLayerWeightRpc(layerIndex, weight);
        }
    }

    [Rpc(SendTo.NotOwner)]
    private void SetLayerWeightRpc(int layerIndex, float weight)
    {
        if (!IsOwner)
        {
            Animator.SetLayerWeight(layerIndex, weight);
        }
    }

}
