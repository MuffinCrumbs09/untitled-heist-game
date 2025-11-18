using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "HasNoSight", story: "[Agent] has no line of sight to [Target]", category: "Conditions", id: "71ddac0271d839c2592e08e091067344")]
public partial class HasNoSightCondition : Condition
{
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    private Sensor _sensor;
    public override bool IsTrue()
    {
        if (Target?.Value == null)
            return true;

        Vector3 dirToTarget = Target.Value.transform.position - Agent.Value.transform.position;
        float dist = dirToTarget.magnitude;

        // Wall/Obstacle in way
        if (Physics.Raycast(Agent.Value.transform.position, dirToTarget, dist, _sensor.obstacleMask))
            return true;

        return false;
    }

    public override void OnStart()
    {
        _sensor = Agent.Value.GetComponent<Sensor>();
    }

    public override void OnEnd()
    {
    }
}
