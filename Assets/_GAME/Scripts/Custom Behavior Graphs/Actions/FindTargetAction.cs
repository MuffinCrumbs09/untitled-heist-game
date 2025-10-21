using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FindTarget", story: "[self] looks for [target]", category: "Action", id: "dfc73de08f6056a7ee6f8c2f3251e096")]
public partial class FindTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    private Sensor sensor;

    protected override Status OnStart()
    {
        sensor = Self.Value.GetComponent<Sensor>();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Target.Value = sensor.InView();

        if (Target.Value != null)
            return Status.Success;
        else
            return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

