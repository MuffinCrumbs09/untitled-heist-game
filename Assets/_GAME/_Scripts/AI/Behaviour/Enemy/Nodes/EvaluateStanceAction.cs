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

        // Invalidate any previously locked stance so AssignStance gets a fresh slot count
        Brain.Value.InvalidateStance();

        ulong targetId = Brain.Value.targetingBrain.currentTargetClientId;
        DistanceState assignedStance = EnemyStanceAssigner.Instance.AssignStance(Brain.Value, targetId);
        Brain.Value.currentDistanceState = assignedStance;

        return Status.Success;
    }
}

