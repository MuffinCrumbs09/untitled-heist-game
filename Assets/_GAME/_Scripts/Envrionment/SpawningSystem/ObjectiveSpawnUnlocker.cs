using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Unlocks specific enemy spawn points when a certain game objective index is reached.
/// </summary>
public class ObjectiveSpawnUnlocker : NetworkBehaviour
{
    #region Variables

    [Header("Objective Link")]
    [SerializeField] 
    [Tooltip("The objective index required to trigger the unlock.")]
    private int ObjectiveIndex;

    [Header("Spawn Points to Unlock")]
    [SerializeField] 
    [Tooltip("List of spawn points that will become active upon reaching the objective.")]
    private EnemySpawnPoint[] spawnPointsToUnlock;

    private bool hasUnlocked = false;

    #endregion

    #region Logic

    /// <summary>
    /// Polls the objective system on the server to check for unlock conditions.
    /// </summary>
    private void Update()
    {
        if (!IsServer || hasUnlocked) return;

        if (ObjectiveSystem.Instance.CurrentObjectiveIndex.Value >= ObjectiveIndex)
        {
            UnlockSpawnPoints();
        }
    }

    /// <summary>
    /// Iterates through linked spawn points and triggers their unlock RPC.
    /// </summary>
    private void UnlockSpawnPoints()
    {
        hasUnlocked = true;
        foreach (EnemySpawnPoint spawnPoint in spawnPointsToUnlock)
        {
            if (spawnPoint != null && spawnPoint.isActiveAndEnabled)
                spawnPoint.UnlockSpawnPointServerRpc();
        }
    }

    /// <summary>
    /// Manual override to unlock spawn points via RPC.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void ManualUnlockServerRpc()
    {
        if (!hasUnlocked) UnlockSpawnPoints();
    }

    #endregion
}