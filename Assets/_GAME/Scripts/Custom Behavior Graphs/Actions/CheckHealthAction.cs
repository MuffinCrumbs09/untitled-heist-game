using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckHealth", story: "[Agent] checks health", category: "Action", id: "7b961f84abff41981fb136789d6b32d2")]
public partial class CheckHealthAction : Action
{
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;

    private Health health;

    protected override Status OnStart()
    {
        health = Agent.Value.GetComponent<Health>();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Debug.Log(health.GetHealth());
                if (health.GetHealth() <= 50)
            return Status.Success;

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

