using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base health component used by generic objects and enemies.
/// Players use PlayerStats (which implements IDamageable directly) — 
/// PlayerHealthController has been removed.
/// </summary>
public class Health : NetworkBehaviour, IDamageable
{
    [Header("Settings - Health")]
    public float maxHealth;
    private NetworkVariable<float> health = new(
        writePerm: NetworkVariableWritePermission.Server
    );

    protected NetworkVariable<bool> isDead = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Default killer id (AI hits use this)
    public const ulong AI_KILLER_ID = ulong.MaxValue;

    [HideInInspector] public UnityEvent<ulong> OnDamaged;

    public void ChangeHealth(float toChange, ulong shooterClientId)
    {
        if (isDead.Value) return;

        if (this is EnemyHealth enemy)
        {
            // Negative toChange = damage; EnemyHealth.TakeDamageRpc expects positive rawDamage
            enemy.TakeDamageRpc(-toChange, false, shooterClientId);
            return;
        }

        // Fallback for any plain Health component (doors, props, etc.)
        ChangeHealthServerRpc(toChange, shooterClientId);
    }

    protected void ApplyHealthChange(float amount)
    {
        health.Value = Mathf.Clamp(health.Value + amount, 0, maxHealth);

        if (health.Value <= 0)
        {
            isDead.Value = true;
            HandleDeath();
        }
    }

    public float GetHealth() => health.Value;

    #region Networking

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ChangeHealthServerRpc(float amount, ulong shooterClientId)
    {
        OnDamaged?.Invoke(shooterClientId);
        ApplyHealthChange(amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void DeadServerRpc()
    {
        isDead.Value = true;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value  = maxHealth;
            isDead.Value  = false;
        }

        isDead.OnValueChanged += OnDeathStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeathStateChanged;
    }

    private void OnDeathStateChanged(bool previous, bool current)
    {
        if (current) HandleDeath();
    }

    protected virtual void HandleDeath()
    {
        if (IsServer)
            NetworkObject.Despawn();
    }

    #endregion
}