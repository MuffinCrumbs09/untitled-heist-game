using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsTargetDead", story: "Check if [Target] is [Dead]", category: "Conditions", id: "5f05adb991c2d04cedc0425115b94d47")]
public partial class IsTargetDeadCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<bool> Dead;

    PlayerHealthController player;

    public override bool IsTrue()
    {
        if(player == null)
            return false;

        return player.IsDead != Dead.Value;
    }

    public override void OnStart()
    {
        player = Target.Value.GetComponent<PlayerHealthController>();
    }

    public override void OnEnd()
    {
    }
}
