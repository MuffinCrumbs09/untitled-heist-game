using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Movement", story: "[Agent] moves to [Target]", category: "Action", id: "2e96ed6b14fc48443e81c0bdca6bc85e")]
public partial class MovementAction : Action
{
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> StoppingDist = new(10);

    protected override Status OnStart()
    {
        if (ReferenceEquals(Target.Value, null))
            return Status.Failure;

        Agent.Value.stoppingDistance = StoppingDist.Value;
        Agent.Value.SetDestination(Target.Value.transform.position);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value.destination != Target.Value.transform.position)
            Agent.Value.SetDestination(Target.Value.transform.position);

        return Vector3.Distance(Agent.Value.transform.position, Target.Value.transform.position) >= StoppingDist.Value ? Status.Running : Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

