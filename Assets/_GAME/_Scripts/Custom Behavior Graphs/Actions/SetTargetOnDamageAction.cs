using System;
using Unity.Behavior;
using Unity.Netcode;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SetTargetOnDamage", story: "[Self] sets [Target] to attacker", category: "Action", id: "e7558e5fa4a28a675e9eb13620f0d49e")]
public partial class SetTargetOnDamageAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    private Health healthComponent;
    private GameObject potentialTarget;

    protected override Status OnStart()
    {
        healthComponent = Self.Value.GetComponent<Health>();
        healthComponent.OnDamaged.AddListener(OnDamagedByAttacker);

        potentialTarget = null;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (potentialTarget != null && (Target.Value == null || Target.Value == Self.Value))
        {
            Target.Value = potentialTarget;
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        healthComponent.OnDamaged.RemoveListener(OnDamagedByAttacker);
    }

    private void OnDamagedByAttacker(ulong shooterClientId)
    {
        // AI_KILLER_ID means the shooter was another AI — no player target to set
        if (shooterClientId == Health.AI_KILLER_ID) return;

        // Resolve the shooter's GameObject from their NetworkClientId
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var client))
        {
            GameObject shooterGO = client.PlayerObject?.gameObject;
            if (shooterGO != null && shooterGO != Self.Value)
                potentialTarget = shooterGO;
        }
    }
}
