using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ExecuteMovement", story: "Agent Executes Movement Using [Brain] Based On [Target]", category: "Action/Enemy/Movement", id: "789d30108955a6727dcd6e709c1026c4")]
public partial class ExecuteMovementAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyMovementBrain> Brain;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    protected override Status OnUpdate()
    {
        if (Target.Value == null)
        {
            Debug.LogError("ExecuteMovementAction failed: Target is null.");
            return Status.Failure;
        }

        DistanceState state = Brain.Value.currentDistanceState;
        Vector3 targetPos = Target.Value.transform.position;

        float distToTarget = Brain.Value.GetNavMeshDistance(targetPos);

        switch (state)
        {
            case DistanceState.Far:
                Brain.Value.ExecuteFarMovement(targetPos, distToTarget);
                break;
            case DistanceState.Mid:
                Brain.Value.ExecuteMidMovement(targetPos, distToTarget);
                break;
            case DistanceState.Close:
                Brain.Value.ExecuteCloseMovement(targetPos, distToTarget);
                break;
            case DistanceState.Strafe:
                Brain.Value.ExecuteStrafeMovement(targetPos, distToTarget);
                break;
            default:
                Debug.LogError($"Unknown distance state {state} in ExecuteMovementAction.");
                return Status.Failure;
        }

        return Status.Running;
    }
}

