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

        [Header("Separation Settings")]
        public float separationRadius = 2.5f;
        public float separationStrength = 2f;

        [Header("Destination Offset")]
        private Vector3 _personalOffset;

        [Header("LOS Settings")]
        public LayerMask wallMask; // Should match Sensor's obstacleMask

        void Awake()
        {
            targetingBrain = GetComponent<EnemyTargetingBrain>();
            sensor = GetComponent<Sensor>();
            agent = GetComponent<NavMeshAgent>();
            _pathBuffer = new NavMeshPath();
            RandomisePersonalOffset();

            // Mirror the sensor's obstacle mask so we don't need to set it twice
            wallMask = sensor != null ? sensor.obstacleMask : wallMask;
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

        public void RandomiseDesiredDistances()
        {
            desiredFarDist    = Random.Range(farMinDist, farMaxDist);
            desiredMidDist    = Random.Range(midMinDist, midMaxDist);
            desiredCloseDist  = Random.Range(closeMinDist, closeMaxDist);
            desiredStrafeDist = Random.Range(strafeMinDist, strafeMaxDist);
            RandomisePersonalOffset();
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

        /// <summary>
        /// Promotes this enemy one tier closer to the player:
        ///   Far → Mid, Mid → Close.
        /// Strafe enemies are left as-is (they're already engaged at mid range).
        /// Randomises desired distances so the new stance feels natural.
        /// </summary>
        public void PromoteStance()
        {
            switch (currentDistanceState)
            {
                case DistanceState.Far:
                    currentDistanceState = DistanceState.Mid;
                    RandomiseDesiredDistances();
                    break;
                case DistanceState.Mid:
                    currentDistanceState = DistanceState.Close;
                    RandomiseDesiredDistances();
                    break;
                // Close and Strafe don't promote further
            }
        }

        public void ExecuteFarMovement(Vector3 targetPos, float distToTarget)
        {
            ResumeMoving();

            bool canSeeTarget = sensor.InView() != null;

            // No LOS — pursue directly, ignore desired position entirely
            if (!canSeeTarget)
            {
                MoveTowardTarget(targetPos, desiredFarDist);
                return;
            }

            // Has LOS — honour band limits and desired distance
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

            float tolerance = 1.5f;
            if (Mathf.Abs(distToTarget - desiredFarDist) > tolerance)
            {
                if (distToTarget > desiredFarDist)
                    MoveTowardTarget(targetPos, desiredFarDist);
                else
                    MoveAwayFromTarget(targetPos, desiredFarDist);
                return;
            }

            StopMoving();
        }

        public void ExecuteMidMovement(Vector3 targetPos, float distToTarget)
        {
            ResumeMoving();

            bool canSeeTarget = sensor.InView() != null;

            // No LOS — pursue directly, ignore desired position entirely
            if (!canSeeTarget)
            {
                MoveTowardTarget(targetPos, desiredMidDist);
                return;
            }

            // Has LOS — honour band limits and desired distance
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

            float tolerance = 1.5f;
            if (Mathf.Abs(distToTarget - desiredMidDist) > tolerance)
            {
                if (distToTarget > desiredMidDist)
                    MoveTowardTarget(targetPos, desiredMidDist);
                else
                    MoveAwayFromTarget(targetPos, desiredMidDist);
                return;
            }

            StopMoving();
        }

        /// <summary>
        /// Raw wall-check raycast to targetPos — ignores sensor cone and viewDist.
        /// Use this for close movement where the sensor cone may not cover the target.
        /// </summary>
        private bool HasDirectLineOfSight(Vector3 targetPos)
        {
            Vector3 dir = targetPos - transform.position;
            return !Physics.Raycast(transform.position, dir.normalized, dir.magnitude, wallMask);
        }

        public void ExecuteCloseMovement(Vector3 targetPos, float distToTarget)
        {
            ResumeMoving();

            // Use a direct raycast rather than the sensor cone — the player may be
            // within close range but outside the sensor's viewDist or view angle
            bool canSeeTarget = HasDirectLineOfSight(targetPos);

            // No LOS — keep pathing around the wall, don't stop
            if (!canSeeTarget)
            {
                MoveTowardTarget(targetPos, desiredCloseDist);
                return;
            }

            // Has LOS — honour band and desired distance
            if (distToTarget > closeMaxDist)
            {
                MoveTowardTarget(targetPos, desiredCloseDist);
                return;
            }

            float tolerance = 1f;
            if (Mathf.Abs(distToTarget - desiredCloseDist) > tolerance)
            {
                if (distToTarget > desiredCloseDist)
                    MoveTowardTarget(targetPos, desiredCloseDist);
                else
                    MoveAwayFromTarget(targetPos, desiredCloseDist);
                return;
            }

            StopMoving();
        }

        public void ExecuteStrafeMovement(Vector3 targetPos, float distanceToTarget)
        {
            ResumeMoving();

            bool canSeeTarget = sensor.InView() != null;

            // No LOS — pursue directly to get into position
            if (!canSeeTarget)
            {
                MoveTowardTarget(targetPos, desiredStrafeDist);
                return;
            }

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

        // Re-roll the lateral offset this enemy uses so no two enemies aim for the exact same spot
        public void RandomisePersonalOffset()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist  = Random.Range(0.5f, 2.5f);
            _personalOffset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
        }

        // Push away from nearby enemies so they don't stack/funnel
        private Vector3 GetSeparationVector()
        {
            Vector3 separation = Vector3.zero;
            Collider[] neighbours = Physics.OverlapSphere(transform.position, separationRadius);
            foreach (var col in neighbours)
            {
                if (col.gameObject == gameObject) continue;
                if (!col.TryGetComponent<EnemyMovementBrain>(out _)) continue;
                Vector3 away = transform.position - col.transform.position;
                float strength = 1f - (away.magnitude / separationRadius);
                separation += away.normalized * strength;
            }
            return separation * separationStrength;
        }

        public void MoveTowardTarget(Vector3 targetPos, float desiredDist)
        {
            Vector3 direction = (targetPos - transform.position).normalized;
            Vector3 baseDestination = targetPos - direction * desiredDist;
            Vector3 destination = baseDestination + _personalOffset + GetSeparationVector();
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