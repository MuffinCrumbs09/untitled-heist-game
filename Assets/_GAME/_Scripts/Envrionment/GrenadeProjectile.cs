using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Networked projectile component for a thrown grenade.
/// Handles physics launch, a timed fuse, area-of-effect explosion damage,
/// and explosion VFX synchronised across all clients.
/// Explosion logic runs exclusively on the server; VFX is sent to all clients via RPC.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GrenadeProjectile : NetworkBehaviour
{
    #region Properties
    [Tooltip("How many seconds after spawning before the grenade explodes.")]
    [SerializeField] private float fuseTime = 3f;

    [Tooltip("The radius (in world units) of the explosion — anything inside this sphere takes damage.")]
    [SerializeField] private float explosionRadius = 5f;

    [Tooltip("The amount of damage dealt to each enemy caught within the explosion radius.")]
    [SerializeField] private float explosionDamage = 125f;

    [Tooltip("The particle/VFX prefab instantiated on every client when the grenade explodes.")]
    [SerializeField] private GameObject explosionVFXPrefab;

    private Rigidbody _rb;
    // The client ID of the player who threw this grenade, used to attribute kill credit.
    private ulong _ownerClientId;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // The fuse countdown is started only on the server to keep explosion logic authoritative.
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        StartCoroutine(FuseCoroutine());
    }
    #endregion

    /// <summary>
    /// Applies an initial velocity to the grenade and records who threw it.
    /// </summary>
    /// <param name="velocity">The world-space velocity to launch the grenade with.</param>
    /// <param name="ownerClientId">The network client ID of the throwing player.</param>
    public void Launch(Vector3 velocity, ulong ownerClientId)
    {
        _ownerClientId = ownerClientId;
        _rb.linearVelocity = velocity;
    }

    /// <summary>
    /// Waits for the fuse duration then triggers the explosion.
    /// Only runs on the server.
    /// </summary>
    private IEnumerator FuseCoroutine()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    /// <summary>
    /// Handles the explosion: spawns VFX on all clients, applies damage to all enemies
    /// within the explosion radius, then despawns the grenade.
    /// Only runs on the server.
    /// </summary>
    private void Explode()
    {
        // Tell every client to play the explosion effect at this position.
        SpawnExplosionVFXClientRpc(transform.position);

        // Find all colliders in the blast radius and damage any enemies hit.
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out EnemyHealth enemy))
                enemy.TakeDamageRpc(explosionDamage, false, _ownerClientId);
        }

        NetworkObject.Despawn();
    }

    /// <summary>
    /// Sent to all clients to instantiate the explosion VFX prefab locally.
    /// The VFX object is destroyed after 4 seconds to avoid scene clutter.
    /// </summary>
    [Rpc(SendTo.Everyone)]
    private void SpawnExplosionVFXClientRpc(Vector3 position)
    {
        GameObject vfx = Instantiate(explosionVFXPrefab, position, Quaternion.identity);
        Destroy(vfx, 4f);
    }

#if UNITY_EDITOR
    // Draws the explosion radius as a translucent sphere in the Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.4f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}