using UnityEngine;
using Unity.Netcode;

public class EnemyStanceAssigner : MonoBehaviour
{
    public static EnemyStanceAssigner Instance { get; private set; }

    [Header("Close Range Quota")]
    [Tooltip("How many enemies per player should be in Close range at any time")]
    public int closeQuota = 25;

    [Header("Mid Range Quota")]
    [Tooltip("After Close is filled, how many should be in Mid range")]
    public int midQuota = 15;

    [Header("Strafe Settings")]
    private const float MAX_STRAFE_RATIO = .4f; // Max % of mid-range enemies that strafe

    private void Awake()
    {
        if (Instance == null || Instance == this)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) this.enabled = false;
    }

    /// <summary>
    /// Assigns a stance to a newly registered or re-evaluating enemy.
    /// Fills Close up to closeQuota, then Mid up to midQuota, then Far.
    /// Mid slots may be replaced with Strafe up to MAX_STRAFE_RATIO.
    /// </summary>
    public DistanceState AssignStance(EnemyMovementBrain enemy, ulong targetClientId)
    {
        enemy.RandomiseDesiredDistances();

        int closeCount = PlayerTargetingManager.Instance.CountEnemiesInStateForPlayer(targetClientId, DistanceState.Close);
        if (closeCount < closeQuota)
            return DistanceState.Close;

        int midCount   = PlayerTargetingManager.Instance.CountEnemiesInStateForPlayer(targetClientId, DistanceState.Mid);
        int strafeCount = PlayerTargetingManager.Instance.CountEnemiesInStateForPlayer(targetClientId, DistanceState.Strafe);
        int combinedMid = midCount + strafeCount;

        if (combinedMid < midQuota)
        {
            // Decide if this mid slot becomes a strafe slot
            int totalEnemies = PlayerTargetingManager.Instance.GetEnemyCountForPlayer(targetClientId);
            float maxStrafe = Mathf.Floor(totalEnemies * MAX_STRAFE_RATIO);

            if (strafeCount < maxStrafe)
            {
                enemy.RandomiseStrafeDir();
                return DistanceState.Strafe;
            }

            return DistanceState.Mid;
        }

        return DistanceState.Far;
    }

    /// <summary>
    /// Legacy entry-point kept for backward compatibility with any existing BT nodes.
    /// Now delegates to AssignStance.
    /// </summary>
    public DistanceState AssignMidStance(EnemyMovementBrain enemy, ulong targetClientId)
    {
        return AssignStance(enemy, targetClientId);
    }
}
