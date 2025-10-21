using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RandomMovement", story: "[Agent] randomly moves to map locations", category: "Action", id: "c8c70cbb58ec67abf28f24ad7f7592ac")]
public partial class RandomMovementAction : Action
{
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;
    private MapManager mapManager;
    private List<GameObject> mapAreas = new();
    private GameObject target;

    protected override Status OnStart()
    {
        mapManager = MapManager.Instance;

        foreach (M_Areas area in mapManager.Areas)
        {
            mapAreas.Add(GameObject.Find(area.Area));
        }

        target = PickRandomArea();

        Debug.Log(target);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        float dist = Vector3.Distance(Agent.Value.transform.position, target.transform.position);
        
        if (dist <= 2.5f)
            return Status.Success;

        if (!Agent.Value.hasPath)
            MoveTo(target);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Agent.Value.ResetPath(); 
    }

    private GameObject PickRandomArea()
    {
        for (int i = 0; i < mapAreas.Count; i++)
        {
            GameObject tmp = mapAreas[i];
            int r = UnityEngine.Random.Range(i, mapAreas.Count);
            mapAreas[i] = mapAreas[r];
            mapAreas[r] = tmp;
        }

        int index = UnityEngine.Random.Range(0, mapAreas.Count);
        return mapAreas[index];
    }

    private void MoveTo(GameObject area)
    {
        Agent.Value.SetDestination(area.transform.position);
    }
}

