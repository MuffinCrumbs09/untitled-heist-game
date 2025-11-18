using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Net;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Strafe", story: "[Agent] strafes around [Target]", category: "Action", id: "a9b0a9f41b1c0383f5c680532756a772")]
public partial class StrafeAction : Action
{
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> StrafeDistance = new(2.0f);
    [SerializeReference] public BlackboardVariable<float> StrafeSpeed = new(3.0f);

    private Vector3 strafeDirection;

    private float prevSpeed, prevStop;

    protected override Status OnStart()
    {
        if (Target == null)
            return Status.Failure;

        prevSpeed = Agent.Value.speed;
        prevStop = Agent.Value.stoppingDistance;
        Agent.Value.speed = StrafeSpeed.Value;
        Agent.Value.stoppingDistance = 0.1f;

        CalculateNewStrafeDirection();
        return Status.Running;
    }
    protected override Status OnUpdate()
    {
        if (Target == null)
            return Status.Failure;

        if (!Agent.Value.pathPending && Agent.Value.remainingDistance <= 1.1f)
        {
            CalculateNewStrafeDirection();
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Agent.Value.speed = prevSpeed;
        Agent.Value.stoppingDistance = prevStop;
        Agent.Value.ResetPath();
    }


    private void CalculateNewStrafeDirection()
    {
        // Calculate direction to target
        Vector3 directionToTarget = Target.Value.transform.position - Agent.Value.transform.position;
        directionToTarget.y = 0;
        directionToTarget.Normalize();

        Vector3 perpendicularDirection = Vector3.Cross(directionToTarget, Vector3.up).normalized;

        // Randomly choose left or right
        strafeDirection = UnityEngine.Random.value > 0.5f ? perpendicularDirection : -perpendicularDirection;

        // Calculate new strafe position
        Vector3 strafeTargetPosition = Target.Value.transform.position + strafeDirection * StrafeDistance.Value;
        if (NavMesh.SamplePosition(strafeTargetPosition, out NavMeshHit hit, StrafeDistance.Value * 2, NavMesh.AllAreas))
        {
            strafeTargetPosition = hit.position;
        }

        // Set new destination
        Agent.Value.SetDestination(strafeTargetPosition);
    }
}

