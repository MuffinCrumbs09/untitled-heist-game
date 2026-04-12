using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ShootTarget", story: "Agent Shoots At [Target] Using [Brain]", category: "Action/Enemy", id: "02c005ed4a8f00a808fa63e91f50432d")]
public partial class ShootTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<EnemyShootingBrain> Brain;

    protected override Status OnStart()
    {
        if (Target.Value == null)
        {
            Brain.Value.CeaseFire();
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Brain.Value.AimAtTarget(Target.Value.transform.position);

        if (Target.Value == null)
        {
            Brain.Value.CeaseFire();
            return Status.Failure;
        }

        // Use the sensor result cached this tick — avoids a second raycast
        bool canSee = Brain.Value.GetComponent<Sensor>().InView() != null;
        Brain.Value.UpdateShooting(canSee, Target.Value.transform.position);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        // Node interrupted or graph moved on — always cease fire cleanly
        Brain.Value.CeaseFire();
    }
}

