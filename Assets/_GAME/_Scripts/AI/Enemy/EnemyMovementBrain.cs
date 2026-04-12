using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public enum DistanceState { Far, Mid, Close, Strafe }

public class EnemyMovementBrain : MonoBehaviour
{
    #region Distances
    [Header("Distance Settings")]
    public float farMinDist = 30f;
    public float farMaxDist = 70f;
    public float midMinDist = 15f;
    public float midMaxDist = 30f;
    public float closeMinDist = 2.1f;
    public float closeMaxDist = 10f;

    public float strafeMinDist = 10f;
    public float strafeMaxDist = 30f;
    #endregion

    [Header("Strafe Settings")]
    public float strafeSpeed = 3f;
    private float strafeAngle;
    private float strafeDir = 1f; // 1 = clockwise, -1 = counterclockwise

    [Header("Desired Distances (set at runtime)")]
    public float desiredFarDist;
    public float desiredMidDist;
    public float desiredCloseDist;
    public float desiredStrafeDist;

    public DistanceState currentDistanceState;
    public EnemyTargetingBrain targetingBrain { get; private set; }

    private Sensor sensor;
    private NavMeshAgent agent;
    private NavMeshPath _pathBuffer;

    void Awake()
    {
        targetingBrain = GetComponent<EnemyTargetingBrain>();
        sensor = GetComponent<Sensor>();
        agent = GetComponent<NavMeshAgent>();
        _pathBuffer = new NavMeshPath();
    }

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) this.enabled = false; // Only the server should run this logic
    }

    public float GetNavMeshDistance(Vector3 targetPos)
    {
        if (agent == null) return float.MaxValue;

        bool found = NavMesh.CalculatePath(transform.position, targetPos, agent.areaMask, _pathBuffer);

        if (!found || _pathBuffer.status == NavMeshPathStatus.PathInvalid)
            return float.MaxValue;

        float dist = 0f;
        Vector3[] corners = _pathBuffer.corners;
        for (int i = 1; i < corners.Length; i++)
            dist += Vector3.Distance(corners[i - 1], corners[i]);

        return dist;
    }

    // Call this once when stance is assigned
    public void RandomiseDesiredDistances()
    {
        desiredFarDist = Random.Range(farMinDist, farMaxDist);
        desiredMidDist = Random.Range(midMinDist, midMaxDist);
        desiredCloseDist = Random.Range(closeMinDist, closeMaxDist);
        desiredStrafeDist = Random.Range(strafeMinDist, strafeMaxDist);
    }

    private DistanceState lockedStance;
    private bool hasLockedStance = false;

    public DistanceState EvaluateStance(float distToTarget)
    {
        if (!hasLockedStance)
        {
            lockedStance = (DistanceState)Random.Range(0, 4);
            hasLockedStance = true;
            RandomiseDesiredDistances();
        }

        return lockedStance;

        // if (distToTarget >= farMinDist) return DistanceState.Far;
        // if (distToTarget >= midMinDist) return DistanceState.Mid;
        // if (distToTarget >= closeMinDist) return DistanceState.Close;
        // return DistanceState.Far;
    }

    public void InvalidateStance()
    {
        hasLockedStance = false;
    }

    public void ExecuteFarMovement(Vector3 targetPos, float distToTarget)
    {
        ResumeMoving();

        // Outside band — always move, regardless of LOS
        if (distToTarget > farMaxDist)
        {
            MoveTowardTarget(targetPos, farMaxDist);
            return;
        }

        if (distToTarget < farMinDist)
        {
            MoveAwayFromTarget(targetPos, farMinDist);
            return;
        }

        // Inside band — now LOS matters
        bool canSeeTarget = sensor.InView() != null;
        float tolerance = 1.5f;

        if (Mathf.Abs(distToTarget - desiredFarDist) > tolerance)
        {
            if (distToTarget > desiredFarDist)
                MoveTowardTarget(targetPos, desiredFarDist);
            else
                MoveAwayFromTarget(targetPos, desiredFarDist);
            return;
        }

        if (canSeeTarget)
            StopMoving();
        else
            MoveTowardTarget(targetPos, desiredFarDist);
    }

    public void ExecuteMidMovement(Vector3 targetPos, float distToTarget)
    {
        ResumeMoving();

        if (distToTarget > midMaxDist)
        {
            MoveTowardTarget(targetPos, midMaxDist);
            return;
        }

        if (distToTarget < midMinDist)
        {
            MoveAwayFromTarget(targetPos, midMinDist);
            return;
        }

        bool canSeeTarget = sensor.InView() != null;
        float tolerance = 1.5f;

        if (Mathf.Abs(distToTarget - desiredMidDist) > tolerance)
        {
            if (distToTarget > desiredMidDist)
                MoveTowardTarget(targetPos, desiredMidDist);
            else
                MoveAwayFromTarget(targetPos, desiredMidDist);
            return;
        }

        if (canSeeTarget)
            StopMoving();
        else
            MoveTowardTarget(targetPos, desiredMidDist);
    }

    public void ExecuteCloseMovement(Vector3 targetPos, float distToTarget)
    {
        ResumeMoving();

        if (distToTarget > closeMaxDist)
        {
            MoveTowardTarget(targetPos, desiredCloseDist);
            return;
        }

        bool canSeeTarget = sensor.InView() != null;
        float tolerance = 1f;

        if (Mathf.Abs(distToTarget - desiredCloseDist) > tolerance)
        {
            if (distToTarget > desiredCloseDist)
                MoveTowardTarget(targetPos, desiredCloseDist);
            else
                MoveAwayFromTarget(targetPos, desiredCloseDist);
            return;
        }

        if (canSeeTarget)
            StopMoving();
        else
            MoveAroundTarget(targetPos);
    }

    public void ExecuteStrafeMovement(Vector3 targetPos, float distanceToTarget)
    {
        ResumeMoving();

        if (distanceToTarget > strafeMaxDist)
        {
            MoveTowardTarget(targetPos, strafeMaxDist);
            return;
        }

        if (distanceToTarget < strafeMinDist)
        {
            MoveAwayFromTarget(targetPos, strafeMinDist);
            return;
        }

        bool canSeeTarget = sensor.InView() != null;

        if (!canSeeTarget)
        {
            MoveTowardTarget(targetPos, desiredStrafeDist);
            return;
        }

        strafeAngle += strafeSpeed * strafeDir * Time.deltaTime;
        if (strafeAngle > 360f) strafeAngle -= 360f;
        if (strafeAngle < 0f) strafeAngle += 360f;

        float rad = strafeAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = new Vector3(
            Mathf.Sin(rad) * desiredStrafeDist,
            0f,
            Mathf.Cos(rad) * desiredStrafeDist
        );

        agent.SetDestination(targetPos + orbitOffset);
    }
    public void RandomiseStrafeDir()
    {
        strafeDir = Random.Range(0, 2) == 0 ? 1 : -1;
    }

    public void MoveTowardTarget(Vector3 targetPos, float desiredDist)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        Vector3 destination = targetPos - direction * desiredDist;
        agent.SetDestination(destination);
    }

    public void MoveAwayFromTarget(Vector3 targetPos, float desiredDist)
    {
        Vector3 direction = (transform.position - targetPos).normalized;
        Vector3 destination = targetPos + direction * desiredDist;
        agent.SetDestination(destination);
    }

    public void MoveTo(Vector3 pos)
    {
        agent.SetDestination(pos);
    }

    public void StopMoving()
    {
        agent.isStopped = true;
    }

    public void ResumeMoving()
    {
        agent.isStopped = false;
    }

    public void MoveAroundTarget(Vector3 targetPos)
    {
        Vector3 toSelf = transform.position - targetPos;
        float currentAngle = Mathf.Atan2(toSelf.x, toSelf.z);
        float targetAngle = currentAngle + (90f * Mathf.Deg2Rad);  // step 90 degrees around

        Vector3 destination = targetPos + new Vector3(
            Mathf.Sin(targetAngle) * closeMaxDist,
            0f,
            Mathf.Cos(targetAngle) * closeMaxDist
        );

        agent.SetDestination(destination);
    }
}