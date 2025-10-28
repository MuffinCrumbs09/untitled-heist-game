using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "LookAtTarget", story: "[Agent] looks at [Target]", category: "Action", id: "0beb2d1c487ecf2a625dd400f2b8179a")]
public partial class LookAtTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    private Vector3 target;

    protected override Status OnStart()
    {
        if (ReferenceEquals(Target.Value, null))
            return Status.Failure;

        target = Vector3.zero;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        target = Target.Value.transform.position - Agent.Value.transform.position;

        Vector3 newDir = Vector3.RotateTowards(Agent.Value.transform.forward, target, Agent.Value.speed, 0.0f);

        Agent.Value.transform.rotation = Quaternion.LookRotation(newDir);

        if (Physics.Raycast(Agent.Value.transform.position, Agent.Value.transform.forward, out RaycastHit hit, 50f))
            if (hit.transform.gameObject == Target.Value.gameObject)
                return Status.Success;

        return Status.Running;
    }

    protected override void OnEnd()
    {
        target = Vector3.zero;
    }
}

