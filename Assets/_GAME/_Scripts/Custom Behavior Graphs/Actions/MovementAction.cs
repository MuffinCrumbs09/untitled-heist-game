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

    private NavMeshAgent _agent;

    protected override Status OnStart()
    {
        if (Target.Value == null) return Status.Failure;

        _agent = Agent.Value;
        _agent.stoppingDistance = StoppingDist.Value;
        _agent.SetDestination(Target.Value.transform.position);

        return Status.Running;
    }

    private const float DestinationUpdateThreshold = 0.5f;

    protected override Status OnUpdate()
    {
        if (Target.Value == null) return Status.Failure;

        float distToDestination = Vector3.Distance(_agent.destination, Target.Value.transform.position);
        if (distToDestination > DestinationUpdateThreshold)
            _agent.SetDestination(Target.Value.transform.position);

        if (_agent.pathPending)
            return Status.Running;

        if (_agent.remainingDistance <= StoppingDist.Value)
            return Status.Success;

        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
            return Status.Failure;

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}
