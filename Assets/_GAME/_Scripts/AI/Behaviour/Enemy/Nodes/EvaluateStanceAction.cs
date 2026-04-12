using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "EvaluateStance", story: "Agent Evaulates Stance Using [Brain] Based On [Target]", category: "Action/Enemy/Movement", id: "d778055c0424dfba7bdad69a681d1e6b")]
public partial class EvaluateStanceAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyMovementBrain> Brain;
    [SerializeReference] public BlackboardVariable<Transform> Target;

    protected override Status OnStart()
    {
        float distToTarget = Brain.Value.GetNavMeshDistance(Target.Value.position);
        DistanceState rawStance = Brain.Value.EvaluateStance(distToTarget);

        Brain.Value.RandomiseDesiredDistances();

        if (rawStance == DistanceState.Mid)
        {
            ulong targetId = Brain.Value.targetingBrain.currentTargetClientId;
            DistanceState assignedStance = EnemyStanceAssigner.Instance.AssignMidStance(Brain.Value, targetId);
            Brain.Value.currentDistanceState = assignedStance;
        }
        else
            Brain.Value.currentDistanceState = rawStance;

        return Status.Success;
    }
}

