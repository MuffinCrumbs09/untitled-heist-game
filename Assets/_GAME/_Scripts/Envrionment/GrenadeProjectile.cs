using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class GrenadeProjectile : NetworkBehaviour
{
    [Header("Fuse")]
    [SerializeField] private float fuseTime = 3f;

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionDamage = 125f;
    [SerializeField] private GameObject explosionVFXPrefab;

    private Rigidbody _rb;
    private ulong _ownerClientId;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        StartCoroutine(FuseCoroutine());
    }

    public void Launch(Vector3 velocity, ulong ownerClientId)
    {
        _ownerClientId = ownerClientId;
        _rb.linearVelocity = velocity;
    }

    private IEnumerator FuseCoroutine()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    private void Explode()
    {
        // Spawn VFX on all clients
        SpawnExplosionVFXClientRpc(transform.position);

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out EnemyHealth enemy))
                enemy.TakeDamageRpc(explosionDamage, false, _ownerClientId);
        }

        // Depspawn
        NetworkObject.Despawn();
    }

    [Rpc(SendTo.Everyone)]
    private void SpawnExplosionVFXClientRpc(Vector3 position)
    {
        GameObject vfx = Instantiate(explosionVFXPrefab, position, Quaternion.identity);
        Destroy(vfx, 4f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.4f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}