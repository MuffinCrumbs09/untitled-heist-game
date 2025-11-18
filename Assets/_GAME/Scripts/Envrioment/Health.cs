using System;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

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

    [HideInInspector] public UnityEvent<GameObject> OnDamaged;

    protected void ApplyHealthChange(int amount)
    {
        health.Value = Mathf.Clamp(health.Value + amount, 0, maxHealth);

        if (health.Value <= 0)
        {
            isDead.Value = true;
            HandleDeath();
        }


        Debug.Log(health.Value);
    }
    public void ChangeHealth(int toChange, GameObject attacker)
    {
        if (isDead.Value) return;

        if (this is PlayerHealthController player)
        {
            player.ResetTime();
            
            if (player.HasShield)
            {
                player.ChangeShieldServerRpc(toChange);
                return;
            }
        }

        if (IsServer)
        {
            ApplyHealthChange(toChange);

            if (TryGetComponent(out AIHitHandler hit))
                hit.RegisterHit(attacker);
        }
        else
        {
            ChangeHealthServerRpc(toChange);
        }
    }

    public float GetHealth()
    {
        return health.Value;
    }

    #region Networking
    [ServerRpc(RequireOwnership = false)]
    private void ChangeHealthServerRpc(int amount)
    {
        ApplyHealthChange(amount);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeadServerRpc()
    {
        isDead.Value = true;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
            isDead.Value = false;
        }

        isDead.OnValueChanged += OnDeathStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeathStateChanged;
    }

    private void OnDeathStateChanged(bool previous, bool current)
    {
        if (current)
            HandleDeath();
    }

    private void HandleDeath()
    {
        if(this is PlayerHealthController player)
            player.HandlePlayerDeathServerRpc();
        else if (IsServer)
            NetworkObject.Despawn();
    }
    #endregion
}
