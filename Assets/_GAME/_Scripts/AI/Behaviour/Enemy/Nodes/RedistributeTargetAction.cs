using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RedistributeTarget", story: "Try Redistribute [Target] Using [Brain]", category: "Action/Targeting", id: "2681142b536a9a04366e4cf6b5279c46")]
public partial class RedistributeTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<EnemyTargetingBrain> Brain;

    protected override Status OnStart()
    {
        float distanceToTarget = Vector3.Distance(Brain.Value.transform.position, Target.Value.transform.position);
        bool changed = Brain.Value.TryRedistirubuteTarget(distanceToTarget);

        if(changed)
            Target.Value = Brain.Value.GetCurrentTargetGameobject();
        
        return Status.Success;
    }
}

