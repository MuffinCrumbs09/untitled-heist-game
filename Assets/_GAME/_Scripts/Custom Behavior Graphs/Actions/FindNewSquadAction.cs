using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections.Generic;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FindNewSquad", story: "[self] finds new [SquadLeader]", category: "Action", id: "3c40a87258cad384470b688321272d3f")]
public partial class FindNewSquadAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> SquadLeader;
    List<GameObject> _SquadLeaders = new();

    protected override Status OnStart()
    {
        foreach (GameObject sl in GameObject.FindGameObjectsWithTag("SquadLeader"))
        {
            // Check if squad isn't full

            // if full, dont add them to list
            _SquadLeaders.Add(sl);
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_SquadLeaders.Count == 0)
            return Status.Failure;


        return FindClosestSquadleader() ? Status.Success : Status.Failure;
    }

    protected override void OnEnd()
    {
    }

    private bool FindClosestSquadleader()
    {
        GameObject closest = null;
        Vector3 selfPosition = Self.Value.transform.position;
        float closestDist = 1000;

        foreach (GameObject sl in _SquadLeaders)
        {
            if(Vector3.Distance(selfPosition, sl.transform.position) < closestDist)
            {
                closest = sl;
            }
        }

        if (closest != null)
        {
            SquadLeader.Value = closest;
            return true;
        }
        else
            return false;
    }
}

