using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RandomiseStrafeDir", story: "Agent Randomises Strafe Using [Brain]", category: "Action/Enemy/Movement", id: "d98282bf79febc0f657a95affcf16ac8")]
public partial class RandomiseStrafeDirAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyMovementBrain> Brain;

    protected override Status OnStart()
    {
        if(Brain.Value.currentDistanceState == DistanceState.Strafe)
        {
            Brain.Value.RandomiseStrafeDir();
        }

        return Status.Success;
    }
}

