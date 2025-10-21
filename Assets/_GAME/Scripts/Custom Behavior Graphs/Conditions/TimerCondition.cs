using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Timer", story: "Wait for [x] seconds", category: "Conditions", id: "0346d42c1967461790d1b214491ee803")]
public partial class TimerCondition : Condition
{
    [SerializeReference] public BlackboardVariable<float> X;

    public override bool IsTrue()
    {
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
