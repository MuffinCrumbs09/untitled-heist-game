using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PickInitalTarget", story: "Agent Picks New [Target] Using [Brain]", category: "Action", id: "e6414cbe719a0b3261d66c97ac020124")]
public partial class PickInitalTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<EnemyTargetingBrain> Brain;
    protected override Status OnStart()
    {
        Brain.Value.PickInitalTarget();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Brain.Value.currentTargetClientId != ulong.MaxValue)
        {
            Target.Value = Brain.Value.GetCurrentTargetGameobject();
            return Status.Success;
        }
        return Status.Running;
    }
}

