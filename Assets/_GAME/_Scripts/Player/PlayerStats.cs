using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    #region Inspector Configuration

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField][Range(1, 32)] private int healthSegments = 4;
    [SerializeField] private float healthRegenRate = 5f;

    [Header("Shield")]
    [SerializeField] private float maxShield = 50f;
    [SerializeField] private float regenCooldown = 5f;
    [SerializeField] private float shieldRegenRate = 10f;
    [SerializeField][Range(0f, 1f)] private float bulletDamageReduction = 0.05f;

    #endregion

    #region Network Variables

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> CurrentShield = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> SegmentLostMask = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #endregion

    #region Server State

    private float[] segmentCurrent;
    private float timeSinceLastHit;
    private bool isShieldRegenerating;
    private bool isHealthRegenerating;

    #endregion

    #region Properties

    public float HealthPerSegment => maxHealth / healthSegments;
    public bool ShieldBroken => CurrentShield.Value <= 0f;
    public bool IsDead => CurrentHealth.Value <= 0f;
    public float MaxHealth => maxHealth;
    public float MaxShield => maxShield;
    public int HealthSegmentCount => healthSegments;
    public bool IsShieldRegenerating => isShieldRegenerating;
    public bool IsHealthRegenerating => isHealthRegenerating;

    #endregion

    #region Events

    public event Action OnStatsChanged;
    public event Action OnDeath;

    #endregion

    #region NGO Lifecycle

    public override void OnNetworkSpawn()
    {
        CurrentHealth.OnValueChanged += HandleHealthChanged;
        CurrentShield.OnValueChanged += HandleShieldChanged;
        SegmentLostMask.OnValueChanged += HandleSegmentMaskChanged;

        if (IsServer)
            ServerInitialise();
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
        CurrentShield.OnValueChanged -= HandleShieldChanged;
        SegmentLostMask.OnValueChanged -= HandleSegmentMaskChanged;
    }

    private void Update()
    {
        if (IsServer)
            HandleRegeneration();
    }

    #endregion

    #region Initialisation

    /// <summary>Sets all segments and NetworkVariables to their starting values.</summary>
    private void ServerInitialise()
    {
        float hps = maxHealth / healthSegments;
        segmentCurrent = new float[healthSegments];

        for (int i = 0; i < healthSegments; i++)
            segmentCurrent[i] = hps;

        CurrentHealth.Value = maxHealth;
        CurrentShield.Value = maxShield;
        SegmentLostMask.Value = 0;
        timeSinceLastHit = regenCooldown;
    }

    #endregion

    #region NetworkVariable Callbacks

    /// <summary>Fires OnStatsChanged and OnDeath when health changes.</summary>
    private void HandleHealthChanged(float previous, float current)
    {
        OnStatsChanged?.Invoke();
        if (current <= 0f && previous > 0f)
            OnDeath?.Invoke();
    }

    private void HandleShieldChanged(float previous, float current) => OnStatsChanged?.Invoke();
    private void HandleSegmentMaskChanged(int previous, int current) => OnStatsChanged?.Invoke();

    #endregion

    #region Damage

    /// <summary>Applies damage server-side. Hits shield first, then health. Can be called from any client.</summary>
    [Rpc(SendTo.Server)]
    public void TakeDamageServerRpc(float amount, bool isBullet = false, bool bypassShield = false)
    {
        if (IsDead) return;

        timeSinceLastHit = 0f;
        isShieldRegenerating = false;
        isHealthRegenerating = false;

        float remaining = amount;

        if (!bypassShield && CurrentShield.Value > 0f)
        {
            if (isBullet)
                remaining *= 1f - bulletDamageReduction;

            float absorbed = Mathf.Min(CurrentShield.Value, remaining);
            CurrentShield.Value -= absorbed;
            remaining -= absorbed;
        }

        if (remaining > 0f)
            ServerApplyHealthDamage(remaining);
    }

    /// <summary>Distributes damage across segments from highest to lowest. Permanently locks any segment emptied.</summary>
    private void ServerApplyHealthDamage(float damage)
    {
        for (int i = healthSegments - 1; i >= 0 && damage > 0f; i--)
        {
            if (segmentCurrent[i] <= 0f) continue;

            float absorbed = Mathf.Min(segmentCurrent[i], damage);
            segmentCurrent[i] -= absorbed;
            damage -= absorbed;

            if (segmentCurrent[i] <= 0f)
            {
                segmentCurrent[i] = 0f;
                SegmentLostMask.Value |= (1 << i);
            }
        }

        ServerRecalculateHealth();
    }

    /// <summary>Sums segmentCurrent and writes the result to the CurrentHealth NetworkVariable.</summary>
    private void ServerRecalculateHealth()
    {
        float total = 0f;
        foreach (float hp in segmentCurrent)
            total += hp;
        CurrentHealth.Value = total;
    }

    #endregion

    #region Regeneration

    /// <summary>Ticks shield regen first, then health regen up to the current segment ceiling. Both share regenCooldown.</summary>
    private void HandleRegeneration()
    {
        if (IsDead) return;

        timeSinceLastHit += Time.deltaTime;
        if (timeSinceLastHit < regenCooldown) return;

        if (CurrentShield.Value < maxShield)
        {
            isShieldRegenerating = true;
            CurrentShield.Value = Mathf.Min(CurrentShield.Value + shieldRegenRate * Time.deltaTime, maxShield);
            return;
        }

        isShieldRegenerating = false;

        float healCeiling = GetHealthHealCeiling();
        if (CurrentHealth.Value >= healCeiling)
        {
            isHealthRegenerating = false;
            return;
        }

        isHealthRegenerating = true;
        float healed = Mathf.Min(CurrentHealth.Value + healthRegenRate * Time.deltaTime, healCeiling);
        ServerApplyHeal(healed - CurrentHealth.Value);
        ServerRecalculateHealth();
    }

    /// <summary>Returns the max health the player can regen to — the top of the highest segment that still has HP.</summary>
    private float GetHealthHealCeiling()
    {
        float hps = maxHealth / healthSegments;

        for (int i = healthSegments - 1; i >= 0; i--)
        {
            bool lost = (SegmentLostMask.Value & (1 << i)) != 0;
            if (lost) continue;
            if (segmentCurrent[i] > 0f)
                return hps * (i + 1);
        }

        return 0f;
    }

    /// <summary>Adds healAmount back into segmentCurrent, filling from the lowest segment upward.</summary>
    private void ServerApplyHeal(float healAmount)
    {
        float hps = maxHealth / healthSegments;

        for (int i = 0; i < healthSegments && healAmount > 0f; i++)
        {
            bool lost = (SegmentLostMask.Value & (1 << i)) != 0;
            if (lost) continue;

            float space = hps - segmentCurrent[i];
            if (space <= 0f) continue;

            float fill = Mathf.Min(space, healAmount);
            segmentCurrent[i] += fill;
            healAmount -= fill;
        }
    }

    #endregion

    #region UI Accessors

    /// <returns>Shield as a 0–1 fraction.</returns>
    public float GetShieldNormalised() => CurrentShield.Value / maxShield;

    /// <returns>Health as a 0–1 fraction.</returns>
    public float GetHealthNormalised() => CurrentHealth.Value / maxHealth;

    /// <summary>Returns per-segment fill fractions and permanently-lost flags, reconstructed from NetworkVariables.</summary>
    public void GetSegmentData(out float[] fillFractions, out bool[] permanentlyLost)
    {
        float hps = maxHealth / healthSegments;
        fillFractions = new float[healthSegments];
        permanentlyLost = new bool[healthSegments];

        float remaining = CurrentHealth.Value;

        for (int i = healthSegments - 1; i >= 0; i--)
        {
            bool lost = (SegmentLostMask.Value & (1 << i)) != 0;
            permanentlyLost[i] = lost;

            if (lost)
            {
                fillFractions[i] = 0f;
            }
            else
            {
                float segmentHP = Mathf.Min(remaining, hps);
                fillFractions[i] = segmentHP / hps;
                remaining -= segmentHP;
            }
        }
    }

    #endregion
}