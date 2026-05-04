using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base networked health component for generic damageable objects. Currently only enemies, but will be expanded to cover
/// destroyable props in the environment
/// Tracks current health as a server-authoritative NetworkVariable, handles damage routing,
/// and fires HandleDeath when health reaches zero.
/// Players do NOT use this — they use PlayerStats. Will be made univseral at a later date
/// </summary>
public class Health : NetworkBehaviour, IDamageable
{
    [Tooltip("The starting and maximum health value for this object. Set in the Inspector.")]
    public float maxHealth;

    // Current health, synchronised from server to all clients.
    // Only the server can write to this value.
    private NetworkVariable<float> health = new(
        writePerm: NetworkVariableWritePermission.Server
    );

    // Tracks whether this object has died. Replicated to all clients so
    // death state is consistent everywhere.
    protected NetworkVariable<bool> isDead = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Default client ID if enemy dies from unknown source.
    public const ulong AI_KILLER_ID = ulong.MaxValue;

    // Fired on the server when this object takes damage, passing the shooter's client ID.
    [HideInInspector] public UnityEvent<ulong> OnDamaged;

    #region Unity Lifecycle
    /// <summary>
    /// Called on all clients when the object is spawned on the network.
    /// Initialises health and subscribes to the isDead change event.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
            isDead.Value = false;
        }

        // Subscribe on all clients so HandleDeath runs everywhere when isDead flips.
        isDead.OnValueChanged += OnDeathStateChanged;
    }

    /// <summary>
    /// Called on all clients when the object is despawned. Unsubscribes from events to prevent leaks.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeathStateChanged;
    }
    #endregion

    /// <summary>
    /// Public entry point for dealing damage or healing this object.
    /// Pass a negative value to deal damage, positive to heal.
    /// Routes to the correct damage method depending on the object type.
    /// </summary>
    /// <param name="toChange">Amount to change health by (negative = damage).</param>
    /// <param name="shooterClientId">The client ID of whoever caused this health change.</param>
    public void ChangeHealth(float toChange, ulong shooterClientId)
    {
        if (isDead.Value) return;

        // EnemyHealth has its own RPC-based damage pipeline — route to it directly
        // to ensure enemy-specific logic (resistances, hit reactions, etc.) runs correctly.
        if (this is EnemyHealth enemy)
        {
            enemy.TakeDamageRpc(-toChange, false, shooterClientId);
            return;
        }

        // For plain Health objects, like props, send a server RPC to apply the change.
        ChangeHealthServerRpc(toChange, shooterClientId);
    }

    /// <summary>
    /// Applies the given health change on the server and triggers death if health hits zero.
    /// Health is clamped between 0 and maxHealth.
    /// </summary>
    /// <param name="amount">The amount to add to current health (negative = damage).</param>
    protected void ApplyHealthChange(float amount)
    {
        health.Value = Mathf.Clamp(health.Value + amount, 0, maxHealth);

        if (health.Value <= 0)
        {
            isDead.Value = true;
            HandleDeath();
        }
    }

    /// <summary>Returns the current health value (server-replicated).</summary>
    public float GetHealth() => health.Value;

    #region Networking
    /// <summary>
    /// Server RPC that applies a health change and fires the OnDamaged event.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ChangeHealthServerRpc(float amount, ulong shooterClientId)
    {
        OnDamaged?.Invoke(shooterClientId);
        ApplyHealthChange(amount);
    }

    /// <summary>
    /// Directly marks this object as dead on the server without going through damage.
    /// Useful for scripted kills or out-of-bounds resets.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void DeadServerRpc()
    {
        isDead.Value = true;
    }

    // Callback for when isDead changes — triggers HandleDeath on the client that receives it.
    private void OnDeathStateChanged(bool previous, bool current)
    {
        if (current) HandleDeath();
    }

    /// <summary>
    /// Called when health reaches zero or DeadServerRpc is invoked.
    /// Base implementation despawns the object from the network.
    /// </summary>
    protected virtual void HandleDeath()
    {
        if (IsServer)
            NetworkObject.Despawn();
    }
    #endregion
}