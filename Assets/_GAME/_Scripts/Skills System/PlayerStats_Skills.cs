using Unity.Netcode;
using UnityEngine;

public partial class PlayerStats : NetworkBehaviour
{
    public float HealthRegenRate       => healthRegenRate;
    public float ShieldRegenRate       => shieldRegenRate;
    public float RegenCooldown         => regenCooldown;
    public float BulletDamageReduction => bulletDamageReduction;

    public void SetMaxHealth(float value)
    {
        float previousMax = maxHealth;
        maxHealth = Mathf.Max(1f, value);

        if (previousMax > 0f && segmentCurrent != null)
        {
            float scale  = maxHealth / previousMax;
            float hpsNew = maxHealth / healthSegments;

            for (int i = 0; i < healthSegments; i++)
                segmentCurrent[i] = Mathf.Min(segmentCurrent[i] * scale, hpsNew);

            ServerRecalculateHealth();
        }
        else
        {
            CurrentHealth.Value = Mathf.Min(CurrentHealth.Value, maxHealth);
        }
    }

    public void SetMaxShield(float value)
    {
        maxShield = Mathf.Max(0f, value);
        CurrentShield.Value = Mathf.Min(CurrentShield.Value, maxShield);
        if (CurrentShield.Value <= 0f)
            _shieldWasBrokenByStats = true;
    }

    public void SetHealthRegenRate(float value)
    {
        healthRegenRate = Mathf.Max(0f, value);
    }

    public void SetShieldRegenRate(float value)
    {
        shieldRegenRate = Mathf.Max(0f, value);
    }

    public void SetRegenCooldown(float value)
    {
        regenCooldown = Mathf.Max(0f, value);
    }

    public void SetBulletDamageReduction(float v)
    {
        bulletDamageReduction = Mathf.Clamp01(v);
    }

    [Rpc(SendTo.Server)]
    public void HealServerRpc(float amount, bool bypassSegmentCeiling = false)
    {
        if (IsDead) return;

        if (bypassSegmentCeiling)
            ServerApplyHealBypass(amount);
        else
            ServerApplyHeal(amount);

        ServerRecalculateHealth();
    }

    private void ServerApplyHealBypass(float healAmount)
    {
        float hps = maxHealth / healthSegments;
        for (int i = 0; i < healthSegments && healAmount > 0f; i++)
        {
            float space = hps - segmentCurrent[i];
            if (space <= 0f) continue;

            float fill = Mathf.Min(space, healAmount);
            segmentCurrent[i] += fill;
            healAmount -= fill;
        }
    }
}