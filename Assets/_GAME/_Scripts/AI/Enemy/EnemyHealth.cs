using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct ArmorLayer
{
    public float MaxAP;
    public float CurrentAP;
    [Range(0f, 1f)]
    public float DamageReduction;
}

public class EnemyHealth : Health
{
    [Header("Armor Settings")]
    [Tooltip("Armor layers are hit top, downwards")]
    public List<ArmorLayer> armorLayers = new();

    private ulong _killerClientId;
    private bool _hasKiller;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        // Initialise armor layers
        for (int i = 0; i < armorLayers.Count; i++)
        {
            var layer = armorLayers[i];
            layer.CurrentAP = layer.MaxAP;
            armorLayers[i] = layer;
        }
    }

    /// <summary>
    /// Primary damage entry point for enemies.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(float rawDamage, bool isHeadshot, ulong shooterClientId)
    {
        if (isDead.Value) return;

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Enemy, $"{"[EnemyHealth]".Color(Color.red)} : dealing {rawDamage} damage", this);
#endif

        // Credit this shooter as the current killer candidate
        _killerClientId = shooterClientId;
        _hasKiller = true;

        // Notify behavior graph / listeners with the shooter id
        OnDamaged?.Invoke(shooterClientId);

        if (isHeadshot)
        {
            ApplyArmorDamage(rawDamage * 0.75f);
            ApplyDirectHealthDamage(rawDamage * 0.25f);
        }
        else
        {
            ApplyArmorDamage(rawDamage);
        }

        if (GetHealth() <= 0)
            DeadServerRpc();
    }

    private void ApplyArmorDamage(float damage)
    {
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Enemy, $"{"[EnemyHealth]".Color(Color.red)} : dealing {damage} damage to armor", this);
#endif
        float remainingDamage = damage;

        while (remainingDamage > 0 && armorLayers.Count > 0)
        {
            ArmorLayer curLayer = armorLayers[0];
            float reducedDamage = remainingDamage * (1f - curLayer.DamageReduction);

            if (curLayer.CurrentAP > reducedDamage)
            {
                curLayer.CurrentAP -= reducedDamage;
                armorLayers[0] = curLayer;
                remainingDamage = 0;
            }
            else
            {
                float damageAbsorbed = curLayer.CurrentAP;
                float rawConsumed = damageAbsorbed / (1f - curLayer.DamageReduction);
                remainingDamage -= rawConsumed;
                armorLayers.RemoveAt(0);
            }
        }

        if (remainingDamage > 0)
            ApplyDirectHealthDamage(remainingDamage);
    }

    private void ApplyDirectHealthDamage(float damage)
    {
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Enemy, $"{"[EnemyHealth]".Color(Color.red)} : dealing {damage} damage to health", this);
#endif
        ApplyHealthChange(-damage);
    }

    protected override void HandleDeath()
    {
        if (IsServer && _hasKiller)
        {
            // Only credit kills to real players, not AI-on-AI
            if (_killerClientId != AI_KILLER_ID && NetPlayerManager.Instance != null)
                NetPlayerManager.Instance.AddKillForPlayer(_killerClientId);

            PlayerTargetingManager.Instance.UnregisterTarget(gameObject);
        }

        base.HandleDeath();
    }
}
