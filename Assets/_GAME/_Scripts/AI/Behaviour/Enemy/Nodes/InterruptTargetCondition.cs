using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "InterruptTarget", story: "Agent Interupted Using [Brain] By [Target]", category: "Conditions", id: "baa103fe9b7129b955ae6914998dc38b")]
public partial class InterruptTargetCondition : Condition
{
    [SerializeReference] public BlackboardVariable<EnemyTargetingBrain> Brain;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    public override bool IsTrue()
    {
        float distanceToTarget = Vector3.Distance(Brain.Value.transform.position, Target.Value.transform.position);
        bool changed = Brain.Value.TryInteruptTarget(distanceToTarget, false);
        return changed;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
        Target.Value = Brain.Value.GetCurrentTargetGameobject();
    }
}
