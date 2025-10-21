using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Retreat", story: "[Agent] retreats from [Target]", category: "Action", id: "86de0ebb3bddbe6e2dd74481a7458c72")]
public partial class RetreatAction : Action
{
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    Vector3 retreatDest;
    bool foundRetreatDest = false;
    float retreatDist = 10f;

    protected override Status OnStart()
    {
        foundRetreatDest = false;

        Vector3 retreatDir = (Agent.Value.transform.position - Target.Value.transform.position).normalized;

        // retreatDest = Agent.Value.transform.position + retreatDir * retreatDist;

        for (int x = 0; x <= 20; x++)
        {
            float randomY = UnityEngine.Random.Range(-1f, 10);

            Vector3 randomYPos = new Vector3(
                Agent.Value.transform.position.x,
                randomY,
                Agent.Value.transform.position.z);

            Vector3 potentialDest = randomYPos + retreatDir * retreatDist;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(potentialDest, out hit, 5f, NavMesh.AllAreas))
            {
                NavMeshPath path = new();
                if (Agent.Value.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    Agent.Value.SetDestination(hit.position);
                    retreatDest = hit.position;
                    foundRetreatDest = true;
                    break;
                }
            }
        }

        return foundRetreatDest ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (!Agent.Value.pathPending && Agent.Value.remainingDistance <= Agent.Value.stoppingDistance + 0.5f)
            return Status.Success;

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (foundRetreatDest)
        {
            Target.Value = null;
            Agent.Value.ResetPath();
        }
    }
}

