using UnityEngine;
using Unity.Netcode;

public class EnemySpawnPoint : NetworkBehaviour
{
    [Header("Spawn Point Settings")]
    [SerializeField] private bool isUnlockedAtStart = true;
    [SerializeField] private float spawnRadius = 1f;

    private NetworkVariable<bool> isUnlocked = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

    public bool IsUnlocked => isUnlocked.Value;

    #region Functions
    public Vector3 GetSpawnPosition()
    {
        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 0;
        return transform.position + randomOffset;
    }

    public Quaternion GetSpawnRotation()
    {
        return transform.rotation;
    }
    #endregion

    #region Networking
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            isUnlocked.Value = isUnlockedAtStart;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UnlockSpawnPointServerRpc()
    {
        if (IsServer)
        {
            isUnlocked.Value = true;
        }
    }
    #endregion

    #region Gizmos
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = isUnlockedAtStart ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
#endif
    #endregion

}
