using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "WaitForHit", story: "[Agent] waits for hit", category: "Action/Enemy", id: "634c556035bebcf9d62ab46659ef6033")]
public partial class WaitForHitAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    private Health health;

    protected override Status OnStart()
    {
        health = Agent.Value.GetComponent<Health>();
        if (health == null)
            return Status.Failure;

            
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

