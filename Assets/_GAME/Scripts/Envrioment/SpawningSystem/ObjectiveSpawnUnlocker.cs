using Unity.Netcode;
using UnityEngine;

public class ObjectiveSpawnUnlocker : NetworkBehaviour
{
    [Header("Objective Link")]
    [SerializeField] private int ObjectiveIndex;

    [Header("Spawn Points to Unlock")]
    [SerializeField] private EnemySpawnPoint[] spawnPointsToUnlock;

    private bool hasUnlocked = false;

    private void Update()
    {
        if (!IsServer || hasUnlocked)
            return;

        if (ObjectiveSystem.Instance.CurrentObjectiveIndex >= ObjectiveIndex)
        {
            UnlockSpawnPoints();
        }
    }

    private void UnlockSpawnPoints()
    {
        hasUnlocked = true;

        foreach (EnemySpawnPoint spawnPoint in spawnPointsToUnlock)
        {
            if (spawnPoint != null && spawnPoint.isActiveAndEnabled)
                spawnPoint.UnlockSpawnPointServerRpc();

        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ManualUnlockServerRpc()
    {
        if (!hasUnlocked)
        {
            UnlockSpawnPoints();
        }
    }
}
