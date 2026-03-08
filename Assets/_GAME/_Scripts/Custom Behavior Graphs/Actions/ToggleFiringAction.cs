using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ToggleFiring", story: "[Agent] sets firing to [IsFiring]", category: "Action", id: "c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8")]
public partial class ToggleFiringAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<bool> IsFiring;

    private AIWeaponInput _weaponInput;

    protected override Status OnStart()
    {
        _weaponInput = Agent.Value.GetComponent<AIWeaponInput>();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _weaponInput.SetFiring(IsFiring.Value);
        
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}
