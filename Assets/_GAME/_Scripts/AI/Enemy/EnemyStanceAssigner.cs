using UnityEngine;
using Unity.Netcode;

public class EnemyStanceAssigner : MonoBehaviour
{
    public static EnemyStanceAssigner Instance { get; private set; }
    private const float MAX_STRAFE_RATIO = .4f; // Max percentage of enemies that can be in strafe stance at once

    private void Awake()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) this.enabled = false; // Only the server should run this logic
    }

    public DistanceState AssignMidStance(EnemyMovementBrain enemy, ulong targetClientId)
    {
        int currentStrafing = CountStrafingEnemiesOnTarget(targetClientId);
        int totalEnemies = PlayerTargetingManager.Instance.GetEnemyCountForPlayer(targetClientId);

        float maxAllowed = Mathf.Floor(totalEnemies * MAX_STRAFE_RATIO);

        enemy.RandomiseDesiredDistances();

        if (currentStrafing < maxAllowed)
        {
            enemy.RandomiseStrafeDir();
            return DistanceState.Strafe;
        }

        return DistanceState.Mid;
    }

    private int CountStrafingEnemiesOnTarget(ulong targetClientId)
    {
        int count = 0;
        foreach (var enemyObj in PlayerTargetingManager.Instance.GetEnemyToTargetMap())
        {
            EnemyMovementBrain movementBrain = enemyObj.Key.GetComponent<EnemyMovementBrain>();
            EnemyTargetingBrain targetingBrain = enemyObj.Key.GetComponent<EnemyTargetingBrain>();

            if (targetingBrain == null || movementBrain == null)
            {
                Debug.LogError($"Enemy {enemyObj.Key.name} is missing required components for stance assignment.");
                continue;
            }


            if (movementBrain.currentDistanceState == DistanceState.Strafe && enemyObj.Value == targetClientId)
            {
                count++;
            }
        }
        return count;
    }
}