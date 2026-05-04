using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Rotates this GameObject to face the local player each frame.
/// Used for world-space UI elements (e.g. name tags, health bars, interaction prompts)
/// that should always be readable regardless of camera angle.
/// </summary>
public class LookAtPlayer : MonoBehaviour
{
    // Cached reference to the local player's transform, set once the NetworkManager is ready.
    private Transform _player;
    // Flag to avoid re-querying the NetworkManager every frame once the player is found.
    private bool _hasSet = false;

    private void Update()
    {
        if (!_hasSet)
        {
            // Wait until the local client's PlayerObject exists in the network session
            // before caching its transform. This handles cases where the player spawns
            // slightly after the scene or this object loads.
            if (NetworkManager.Singleton != null
                && NetworkManager.Singleton.LocalClient != null
                && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                _player = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
                _hasSet = true;
            }
            else
                return;
        }

        // Rotate this object so it always faces the local player
        transform.LookAt(_player);
    }
}