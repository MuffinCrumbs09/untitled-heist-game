using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Represents a physical location in the world where enemies can be instantiated.
/// </summary>
public class EnemySpawnPoint : NetworkBehaviour
{
    #region Variables

    [Header("Spawn Point Settings")]
    [SerializeField] 
    [Tooltip("Is this spawn point available as soon as the game starts?")]
    private bool isUnlockedAtStart = true;

    [SerializeField] 
    [Tooltip("The radius around this point where enemies can appear.")]
    private float spawnRadius = 1f;

    private NetworkVariable<bool> isUnlocked = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsUnlocked => isUnlocked.Value;

    #endregion

    #region Utility Functions

    /// <summary> Calculates a random position within the defined spawn radius. </summary>
    public Vector3 GetSpawnPosition()
    {
        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 0;
        return transform.position + randomOffset;
    }

    /// <summary> Returns the rotation of the spawn point. </summary>
    public Quaternion GetSpawnRotation() => transform.rotation;

    #endregion

    #region Networking

    /// <summary> Initializes the unlocked state on the server. </summary>
    public override void OnNetworkSpawn()
    {
        if (IsServer) isUnlocked.Value = isUnlockedAtStart;
    }

    /// <summary> Server RPC to unlock this spawn point for future waves. </summary>
    [Rpc(SendTo.Server)]
    public void UnlockSpawnPointServerRpc()
    {
        if (IsServer) isUnlocked.Value = true;
    }

    #endregion

    #region Editor Visualization

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