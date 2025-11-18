using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Linq;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "GetAllPlayers", story: "Populate [PlayerList]", category: "Action", id: "6e8daad8c8991073c32ccdbba3f00cdf")]
public partial class GetAllPlayersAction : Action
{
    [SerializeReference] public BlackboardVariable<List<GameObject>> PlayerList;

    protected override Status OnStart()
    {
        PlayerList.Value = GameObject.FindGameObjectsWithTag("Player").ToList();
        return PlayerList.Value != null && PlayerList.Value.Count > 0 ? Status.Success : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

