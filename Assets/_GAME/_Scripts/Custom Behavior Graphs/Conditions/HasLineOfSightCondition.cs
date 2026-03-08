using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "HasLineOfSight", story: "Does [Agent] have line of sight to [Target]", category: "Conditions", id: "4748ba74b1031bc69cd6e222f1d6ea2e")]
public partial class HasLineOfSightCondition : Condition
{
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    private float MaxDist = 50f;
    private Sensor _sensor;

    public override bool IsTrue()
    {
        if (Target?.Value == null)
            return false;
        
        Vector3 dirToTarget = Target.Value.transform.position - Agent.Value.transform.position;
        float dist = dirToTarget.magnitude;

        // Too Far Away
        if (dist > MaxDist)
            return false;

        // Wall/Obstacle in way
        if (Physics.Raycast(Agent.Value.transform.position, dirToTarget, dist, _sensor.obstacleMask))
            return false;

        return true;
    }

    public override void OnStart()
    {
        _sensor = Agent.Value.GetComponent<Sensor>();
    }

    public override void OnEnd()
    {
    }
}
