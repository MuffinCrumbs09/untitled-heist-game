using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Teleports a player back to a designated spawn point if they leave the playable area.
/// Uses a trigger collider to detect when a player has gone out of bounds.
/// </summary>
public class OutOfBounds : MonoBehaviour
{
    [Tooltip("The position the player is teleported to when they go out of bounds.\nAssign a Transform in the scene (e.g. a safe respawn point near the level entrance).")]
    public Transform spawnPoint;

    /// <summary>
    /// Fires when a collider enters this trigger zone.
    /// Checks if it is the local player and if so, teleports them back to the spawn point.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out NetworkObject networkObject))
            {
                // Only the owner of this player object should handle the teleport —
                if (!networkObject.IsOwner)
                    return;

                // Disable movement so the player can't move during the teleport frame.
                networkObject.GetComponent<PlayerMovement>().enabled = false;
                networkObject.transform.position = spawnPoint.position;

                // Re-enable movement after a short delay so the teleport settles.
                StartCoroutine(AllowMovement(networkObject));
            }
        }
    }

    /// <summary>
    /// Re-enables the player's movement script after a short delay post-teleport.
    /// </summary>
    private IEnumerator AllowMovement(NetworkObject obj)
    {
        yield return new WaitForSeconds(1f);
        obj.GetComponent<PlayerMovement>().enabled = true;
    }
}