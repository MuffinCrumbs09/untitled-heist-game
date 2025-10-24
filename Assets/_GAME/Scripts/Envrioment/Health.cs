using System;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private float maxHealth;
    private NetworkVariable<float> health = new(
        writePerm: NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isDead = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void ApplyHealthChange(int amount)
    {
        if (isDead.Value) return;

        health.Value = Mathf.Clamp(health.Value + amount, 0, maxHealth);

        if (health.Value <= 0)
        {
            isDead.Value = true;
            HandleDeath();
        }

        Debug.Log(health.Value);
    }
    public void ChangeHealth(int toChange)
    {
        if (IsServer)
        {
            ApplyHealthChange(toChange);
        }
        else
        {
            ChangeHealthServerRpc(toChange);
        }
    }

    private void ValueChange(bool previous, bool current)
    {
        Destroy(gameObject);
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
        isDead.OnValueChanged -= ValueChange;
    }

    private void OnDeathStateChanged(bool previous, bool current)
    {
        if (current)
            HandleDeath();
    }

    private void HandleDeath()
    {
        if (IsServer)
        {
            if (TryGetComponent(out NetworkObject netObj))
            {
                NetworkObject.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject, 2f);
        }
    }
    #endregion
}
