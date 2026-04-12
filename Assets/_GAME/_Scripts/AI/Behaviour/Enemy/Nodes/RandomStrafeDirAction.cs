using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RandomStrafeDir", story: "Agent Randomises Strafe Distance Using [Brain]", category: "Action/Enemy/Movement", id: "3d4cd860be67188e5bd7197a417ac769")]
public partial class RandomStrafeDirAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyMovementBrain> Brain;

    protected override Status OnStart()
    {
        if (Brain.Value.currentDistanceState == DistanceState.Strafe)
        {
            Brain.Value.RandomiseStrafeDir();
        }

        return Status.Success;
    }
}

