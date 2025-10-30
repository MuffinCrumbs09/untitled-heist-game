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

    protected override Status OnStart()
    {
        if (Target == null)
            return Status.Failure;
        
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
        Agent.Value.ResetPath();
    }


    private void CalculateNewStrafeDirection()
    {
        // Calculate direction to target
        Vector3 directionToTarget = (Target.Value.transform.position - Agent.Value.transform.position).normalized;
        Vector3 perpendicularDirection = Vector3.Cross(directionToTarget, Vector3.up).normalized;

        // Randomly choose left or right
        strafeDirection = UnityEngine.Random.value > 0.5f ? perpendicularDirection : -perpendicularDirection;

        // Calculate new strafe position
        Vector3 strafePosition = Agent.Value.transform.position + strafeDirection * StrafeDistance.Value;

        // Set new destination
        Agent.Value.SetDestination(strafePosition);
    }
}

