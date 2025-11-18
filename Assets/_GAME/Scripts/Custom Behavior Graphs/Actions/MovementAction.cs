using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Unity.VisualScripting;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Movement", story: "[Agent] moves to [Target]", category: "Action", id: "2e96ed6b14fc48443e81c0bdca6bc85e")]
public partial class MovementAction : Action
{
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> StoppingDist = new(10);

    private NavMeshAgent _agent;

    protected override Status OnStart()
    {
        if (Target.Value == null) return Status.Failure;

        _agent = Agent.Value;

        _agent.stoppingDistance = StoppingDist.Value;
        _agent.SetDestination(Target.Value.transform.position);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_agent.destination != Target.Value.transform.position)
            _agent.SetDestination(Target.Value.transform.position);

        return Vector3.Distance(_agent.transform.position, Target.Value.transform.position) >= StoppingDist.Value ? Status.Running : Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

