using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SetNewTarget", story: "Set [Target] from [Playerlist]", category: "Action/Enemy", id: "0693a6714c2d545013592de5c3feecec")]
public partial class SetNewTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<List<GameObject>> Playerlist;

    protected override Status OnStart()
    {
        if (Target.Value != null) return Status.Success;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Playerlist.Value == null || Playerlist.Value.Count == 0) return Status.Running;

        int randomIndex = UnityEngine.Random.Range(0, Playerlist.Value.Count);
        Target.Value = Playerlist.Value[randomIndex];

        return Target.Value != null ? Status.Success : Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

