using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSkillTree : NetworkBehaviour
{
    [Header("Config")]
    [Tooltip("Assign the project-wide SkillTreeConfig asset here.")]
    public SkillTreeConfig Config;

    private NetworkVariable<int> selectedSkillsMask = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private PlayerStats _stats;
    private Gun _gun;

    public event Action<int> OnSkillSelectionChanged;

    private float _baseMaxHealth;
    private float _baseHealthRegenRate;
    private float _baseMaxShield;
    private float _baseShieldRegenRate;
    private float _baseRegenCooldown;
    private float _baseBulletDamageReduction;

    private float _baseDamage;
    private float _baseFireRate;
    private float _baseReloadTime;
    private float _baseMagazineSize;

    private bool _basesRecorded = false;
    private bool _hardenedPlateSoak = false;

    // Shorthand for logging — includes client ID in every message
    private string Client => $"Client {OwnerClientId}";

    public override void OnNetworkSpawn()
    {
        _stats = GetComponentInChildren<PlayerStats>();
        _gun   = GetComponentInChildren<Gun>(true);

        selectedSkillsMask.OnValueChanged += OnMaskChanged;

        StartCoroutine(DelayedCaptureAndApply());
    }

    public override void OnNetworkDespawn()
    {
        selectedSkillsMask.OnValueChanged -= OnMaskChanged;
    }

    private System.Collections.IEnumerator DelayedCaptureAndApply()
    {
        yield return null;

        CaptureBaselines();

        if (IsServer)
        {
            selectedSkillsMask.Value = GetStoredMaskForOwner();
        }
        else
        {
            ApplySkills(selectedSkillsMask.Value);
        }
    }

    public bool HasSkill(SkillType type) =>
        (selectedSkillsMask.Value & (1 << (int)type)) != 0;

    public int PointsSpent()
    {
        int mask = selectedSkillsMask.Value, count = 0;
        while (mask != 0) { count += mask & 1; mask >>= 1; }
        return count;
    }

    public int PointsRemaining() => Config.TotalPoints - PointsSpent();

    private void OnMaskChanged(int previous, int current)
    {
        ApplySkills(current);
        OnSkillSelectionChanged?.Invoke(current);
    }

    private void ApplySkills(int mask)
    {
        if (!_basesRecorded) return;

        ResetToBaselines();

        foreach (SkillType type in Enum.GetValues(typeof(SkillType)))
        {
            if ((mask & (1 << (int)type)) == 0) continue;
            ApplySingleSkill(type);
        }
    }

    private void ApplySingleSkill(SkillType type)
    {
        switch (type)
        {
            case SkillType.FMJ:
                if (_gun != null)
                {
                    _gun.GunData.Damage = _baseDamage * 1.25f;
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Player,
                        $"[Skill] {Client} — FMJ applied: Damage {_baseDamage:F1} → {_gun.GunData.Damage:F1} (+25%)",
                        this);
#endif
                }
                break;

            case SkillType.Overclocked:
                if (_gun != null)
                {
                    _gun.GunData.FireRate = _baseFireRate * 1.40f;
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Player,
                        $"[Skill] {Client} — Overclocked applied: FireRate {_baseFireRate:F2} → {_gun.GunData.FireRate:F2} (+40%)",
                        this);
#endif
                }
                break;

            case SkillType.SpeedCola:
                if (_gun != null)
                {
                    _gun.GunData.ReloadTime = _baseReloadTime * 0.60f;
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Player,
                        $"[Skill] {Client} — SpeedCola applied: ReloadTime {_baseReloadTime:F2}s → {_gun.GunData.ReloadTime:F2}s (-40%)",
                        this);
#endif
                }
                break;

            case SkillType.ExtendedMag:
                if (_gun != null)
                {
                    _gun.GunData.MagazineSize = Mathf.Round(_baseMagazineSize * 1.50f);
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Player,
                        $"[Skill] {Client} — ExtendedMag applied: MagazineSize {_baseMagazineSize:F0} → {_gun.GunData.MagazineSize:F0} (+50%)",
                        this);
#endif
                }
                break;

            case SkillType.Tough:
                if (_stats != null)
                {
                    _stats.SetMaxHealth(_baseMaxHealth + 50f);
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Player,
                        $"[Skill] {Client} — Tough applied: MaxHealth {_baseMaxHealth:F0} → {_stats.MaxHealth:F0} (+50)",
                        this);
#endif
                }
                break;

            case SkillType.Medic:
                if (_stats != null)
                {
                    _stats.SetHealthRegenRate(_baseHealthRegenRate * 2f);
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Player,
                        $"[Skill] {Client} — Medic applied: HealthRegenRate {_baseHealthRegenRate:F2} → {_stats.HealthRegenRate:F2} (x2)",
                        this);
#endif
                }
                break;

            case SkillType.LastStand:
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Player,
                    $"[Skill] {Client} — LastStand registered (triggers on shield break: heal 50% max HP)",
                    this);
#endif
                break;

            case SkillType.Adrenaline:
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Player,
                    $"[Skill] {Client} — Adrenaline registered (triggers at <30% HP: +25% sprint speed)",
                    this);
#endif
                break;

            case SkillType.HardenedPlate:
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Player,
                    $"[Skill] {Client} — HardenedPlate registered (triggers on full shield regen: absorb next hit)",
                    this);
#endif
                break;

            case SkillType.Bulletproof:
                if (_stats != null)
                {
                    _stats.SetBulletDamageReduction(_baseBulletDamageReduction + 0.15f);
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Player,
                        $"[Skill] {Client} — Bulletproof applied: DamageReduction {_baseBulletDamageReduction:P0} → {_stats.BulletDamageReduction:P0} (+15%)",
                        this);
#endif
                }
                break;

            case SkillType.RegenerativePlate:
                if (_stats != null)
                {
                    _stats.SetShieldRegenRate(_baseShieldRegenRate * 2f);
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Player,
                        $"[Skill] {Client} — RegenerativePlate applied: ShieldRegenRate {_baseShieldRegenRate:F2} → {_stats.ShieldRegenRate:F2} (x2)",
                        this);
#endif
                }
                break;

            case SkillType.QuickPlate:
                if (_stats != null)
                {
                    _stats.SetRegenCooldown(Mathf.Max(0f, _baseRegenCooldown - 2f));
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Player,
                        $"[Skill] {Client} — QuickPlate applied: RegenCooldown {_baseRegenCooldown:F1}s → {_stats.RegenCooldown:F1}s (-2s)",
                        this);
#endif
                }
                break;
        }
    }

    public void OnShieldBroken()
    {
        if (!HasSkill(SkillType.LastStand) || _stats == null) return;

        float healAmount = _stats.MaxHealth * 0.50f;
        _stats.HealServerRpc(healAmount, bypassSegmentCeiling: true);
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Player,
            $"[Skill] {Client} — LastStand triggered: shield broke, healing {healAmount:F1} HP",
            this);
#endif
    }

    public float GetAdrenalineSpeedMultiplier()
    {
        if (!HasSkill(SkillType.Adrenaline) || _stats == null) return 1f;

        float healthFraction = _stats.CurrentHealth.Value / _stats.MaxHealth;
        bool active = healthFraction < 0.30f;
#if UNITY_EDITOR
        if (active)
            LoggerEvent.Log(LogPrefix.Player,
                $"[Skill] {Client} — Adrenaline triggered: HP at {healthFraction:P0}, speed x1.25",
                this);
#endif
        return active ? 1.25f : 1f;
    }

    public bool TryHardenedPlateAbsorb()
    {
        if (!HasSkill(SkillType.HardenedPlate) || !_hardenedPlateSoak) return false;

        _hardenedPlateSoak = false;
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Player,
            $"[Skill] {Client} — HardenedPlate triggered: absorbed incoming hit (soak consumed)",
            this);
#endif
        return true;
    }

    public void OnShieldFullyRegenerated()
    {
        if (HasSkill(SkillType.HardenedPlate))
        {
            _hardenedPlateSoak = true;
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Player,
                $"[Skill] {Client} — HardenedPlate ready: shield fully regenerated, next hit will be absorbed",
                this);
#endif
        }
    }

    private void CaptureBaselines()
    {
        if (_stats != null)
        {
            _baseMaxHealth             = _stats.MaxHealth;
            _baseHealthRegenRate       = _stats.HealthRegenRate;
            _baseMaxShield             = _stats.MaxShield;
            _baseShieldRegenRate       = _stats.ShieldRegenRate;
            _baseRegenCooldown         = _stats.RegenCooldown;
            _baseBulletDamageReduction = _stats.BulletDamageReduction;
        }

        if (_gun != null && _gun.GunData != null)
        {
            _gun.GunData = Instantiate(_gun.GunData);

            _baseDamage       = _gun.GunData.Damage;
            _baseFireRate     = _gun.GunData.FireRate;
            _baseReloadTime   = _gun.GunData.ReloadTime;
            _baseMagazineSize = _gun.GunData.MagazineSize;
        }

        _basesRecorded = true;
    }

    private void ResetToBaselines()
    {
        if (_stats != null)
        {
            _stats.SetMaxHealth(_baseMaxHealth);
            _stats.SetMaxShield(_baseMaxShield);
            _stats.SetHealthRegenRate(_baseHealthRegenRate);
            _stats.SetShieldRegenRate(_baseShieldRegenRate);
            _stats.SetRegenCooldown(_baseRegenCooldown);
            _stats.SetBulletDamageReduction(_baseBulletDamageReduction);
        }

        if (_gun != null && _gun.GunData != null)
        {
            _gun.GunData.Damage       = _baseDamage;
            _gun.GunData.FireRate     = _baseFireRate;
            _gun.GunData.ReloadTime   = _baseReloadTime;
            _gun.GunData.MagazineSize = _baseMagazineSize;
        }
    }

    private int GetStoredMaskForOwner()
    {
        var npm = NetPlayerManager.Instance;
        for (int i = 0; i < npm.playerData.Count; i++)
            if (npm.playerData[i].CLIENTID == OwnerClientId)
                return npm.playerData[i].SKILLS;
        return 0;
    }

    private void OnApplicationQuit() => ResetToBaselines();
    private void OnDisable()         => ResetToBaselines();
}