using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "InterruptTargetCheck", story: "Agent Checks Interupt Of [Target] Using [Brain]", category: "Action/Targeting", id: "1542905e880786f3840c1bceec1f8b62")]
public partial class InterruptTargetCheckAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<EnemyTargetingBrain> Brain;

    protected override Status OnStart()
    {
        float distanceToTarget = Vector3.Distance(Brain.Value.transform.position, Target.Value.transform.position);
        bool changed = Brain.Value.TryInteruptTarget(distanceToTarget, false);

        if (changed)
        {
            Target.Value = Brain.Value.GetCurrentTargetGameobject();
            return Status.Success;
        }

        return Status.Failure;
    }
}

