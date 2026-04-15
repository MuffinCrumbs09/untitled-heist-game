using System;
using Unity.Netcode;
using UnityEngine;

public partial class PlayerStats : NetworkBehaviour, IDamageable
{
    #region Inspector

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField][Range(1, 32)] private int healthSegments = 4;
    [SerializeField] private float healthRegenRate = 5f;

    [Header("Shield")]
    [SerializeField] private float maxShield = 50f;
    [SerializeField] private float regenCooldown = 5f;
    [SerializeField] private float shieldRegenRate = 10f;
    [SerializeField][Range(0f, 1f)] private float bulletDamageReduction = 0.05f;

    [Header("Renderers")]
    public SkinnedMeshRenderer PlayerMesh;
    public MeshRenderer[] ExtraMeshRenderers;

    #endregion

    #region Network State

    public NetworkVariable<float> CurrentHealth = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> CurrentShield = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> SegmentLostMask = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> _isDead = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #endregion

    #region Runtime State (Server Only)

    private float[] segmentCurrent;
    private float timeSinceLastHit;
    private bool isShieldRegenerating;
    private bool isHealthRegenerating;
    private bool _shieldWasBrokenByStats;

    private const float RESPAWN_DELAY = 7.5f;

    #endregion

    #region External References

    public PlayerSkillTree SkillTree;

    #endregion

    #region Properties

    public float HealthPerSegment => maxHealth / healthSegments;
    public bool ShieldBroken => CurrentShield.Value <= 0f;
    public bool IsDead => _isDead.Value;
    public float MaxHealth => maxHealth;
    public float MaxShield => maxShield;
    public int HealthSegmentCount => healthSegments;
    public bool IsShieldRegenerating => isShieldRegenerating;
    public bool IsHealthRegenerating => isHealthRegenerating;
    public bool HasShield => CurrentShield.Value > 0f;

    #endregion

    #region Events

    public event Action OnStatsChanged;
    public event Action OnDeath;

    #endregion

    #region Unity / NGO Lifecycle

    public override void OnNetworkSpawn()
    {
        CurrentHealth.OnValueChanged += HandleHealthChanged;
        CurrentShield.OnValueChanged += HandleShieldChanged;
        SegmentLostMask.OnValueChanged += HandleSegmentMaskChanged;
        _isDead.OnValueChanged += HandleDeadChanged;

        if (!IsOwner)
        {
            if (PlayerMesh != null)
                PlayerMesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            foreach (var r in ExtraMeshRenderers)
                if (r != null)
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            ToggleRenderers(true);
        }

        if (IsServer)
            ServerInitialise();
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
        CurrentShield.OnValueChanged -= HandleShieldChanged;
        SegmentLostMask.OnValueChanged -= HandleSegmentMaskChanged;
        _isDead.OnValueChanged -= HandleDeadChanged;
    }

    private void Update()
    {
        if (!IsOwner)
            ToggleRenderers(!IsDead);

        if (IsServer)
            HandleRegeneration();
    }

    #endregion

    #region Initialisation

    private void ServerInitialise()
    {
        float hps = maxHealth / healthSegments;
        segmentCurrent = new float[healthSegments];

        for (int i = 0; i < healthSegments; i++)
            segmentCurrent[i] = hps;

        CurrentHealth.Value = maxHealth;
        CurrentShield.Value = maxShield;
        SegmentLostMask.Value = 0;
        _isDead.Value = false;
        timeSinceLastHit = regenCooldown;
        _shieldWasBrokenByStats = false;
    }

    #endregion

    #region Damage Interface

    public void ChangeHealth(float toChange, ulong shooterClientId)
    {
        if (IsDead) return;

        TakeDamageServerRpc(-toChange, true, false);
    }

    #endregion

    #region Damage Processing

    [Rpc(SendTo.Server)]
    public void TakeDamageServerRpc(float amount, bool isBullet = false, bool bypassShield = false)
    {
        if (IsDead) return;

        timeSinceLastHit = 0f;
        isShieldRegenerating = false;
        isHealthRegenerating = false;

        float remaining = Mathf.Max(0f, amount);

        if (!bypassShield && CurrentShield.Value > 0f)
        {
            if (SkillTree != null && SkillTree.TryHardenedPlateAbsorb())
                return;

            if (isBullet)
                remaining *= 1f - bulletDamageReduction;

            float absorbed = Mathf.Min(CurrentShield.Value, remaining);
            CurrentShield.Value -= absorbed;
            remaining -= absorbed;

            if (CurrentShield.Value <= 0f && !_shieldWasBrokenByStats)
            {
                _shieldWasBrokenByStats = true;
                SkillTree?.OnShieldBroken();
            }
        }

        if (remaining > 0f)
            ServerApplyHealthDamage(remaining);
    }

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

        if (CurrentHealth.Value <= 0f && !_isDead.Value)
            _isDead.Value = true;
    }

    #endregion

    #region Healing & Regeneration

    private void HandleRegeneration()
    {
        if (IsDead) return;

        timeSinceLastHit += Time.deltaTime;
        if (timeSinceLastHit < regenCooldown) return;

        if (CurrentShield.Value < maxShield)
        {
            isShieldRegenerating = true;

            float newShield = CurrentShield.Value + shieldRegenRate * Time.deltaTime;
            CurrentShield.Value = Mathf.Min(newShield, maxShield);

            if (CurrentShield.Value >= maxShield && _shieldWasBrokenByStats)
            {
                _shieldWasBrokenByStats = false;
                SkillTree?.OnShieldFullyRegenerated();
            }

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

        float healAmount = healthRegenRate * Time.deltaTime;
        ServerApplyHeal(healAmount);
        ServerRecalculateHealth();
    }

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

    private float GetHealthHealCeiling()
    {
        float hps = maxHealth / healthSegments;

        for (int i = healthSegments - 1; i >= 0; i--)
        {
            bool lost = (SegmentLostMask.Value & (1 << i)) != 0;

            if (!lost && segmentCurrent[i] > 0f)
                return hps * (i + 1);
        }

        return hps;
    }

    #endregion

    #region State Recalculation

    private void ServerRecalculateHealth()
    {
        float total = 0f;

        for (int i = 0; i < segmentCurrent.Length; i++)
            total += segmentCurrent[i];

        CurrentHealth.Value = total;
    }

    #endregion

    #region Respawn

    private System.Collections.IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(RESPAWN_DELAY);

        if (!IsServer) yield break;

        ServerInitialise();
    }

    #endregion

    #region Rendering

    private void ToggleRenderers(bool enabled)
    {
        if (PlayerMesh != null) PlayerMesh.enabled = enabled;

        foreach (var r in ExtraMeshRenderers)
            if (r != null)
                r.enabled = enabled;
    }

    #endregion

    #region Network Callbacks

    private void HandleHealthChanged(float previous, float current)
    {
        OnStatsChanged?.Invoke();

        if (current <= 0f && previous > 0f)
            OnDeath?.Invoke();
    }

    private void HandleShieldChanged(float previous, float current)
    {
        OnStatsChanged?.Invoke();
    }

    private void HandleSegmentMaskChanged(int previous, int current)
    {
        OnStatsChanged?.Invoke();
    }

    private void HandleDeadChanged(bool previous, bool current)
    {
        if (current)
            StartCoroutine(RespawnCoroutine());
    }

    #endregion

    #region UI / Queries

    public float GetShieldNormalised() => maxShield > 0f ? CurrentShield.Value / maxShield : 0f;
    public float GetHealthNormalised() => maxHealth > 0f ? CurrentHealth.Value / maxHealth : 0f;

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