using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;

public class PlayerHealthController : Health
{
    [Header("Settings - Shield")]
    public int MaxShield;
    public float ShieldRegenTime;
    public int ShieldRegenAmount;

    public SkinnedMeshRenderer PlayerMesh;
    public MeshRenderer[] meshRenderers;
    private float time;

    public bool HasShield { private set; get; } = true;
    public bool IsDead => isDead.Value;
    private NetworkVariable<float> shield = new(
        writePerm: NetworkVariableWritePermission.Server
    );

    private void Start()
    {
        if (!IsServer) return;

        shield.Value = MaxShield;
        isDead.OnValueChanged += DeadStateChanged;
    }

    private void DeadStateChanged(bool previousValue, bool newValue)
    {
        if (newValue)
            StartCoroutine(WaitForAlive());
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner)
        {   
            PlayerMesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var mesh in meshRenderers)
                mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            ToggleRenderers(true);
        }
    }

    private void Update()
    {
        if (!IsOwner) { ToggleRenderers(!IsDead);  return; }

        if (shield.Value < MaxShield)
            RegenerateShield();
    }

    private void ToggleRenderers(bool enabled)
    {
        PlayerMesh.enabled = enabled;
        foreach (var mesh in meshRenderers)
            mesh.enabled = enabled;
    }

    public void ResetTime()
    {
        time = 0;
    }

    public float GetShield()
    {
        return shield.Value;
    }

    private void RegenerateShield()
    {
        time += Time.deltaTime;

        if (time >= ShieldRegenTime)
        {
            ChangeShieldServerRpc(ShieldRegenAmount);
            time = 0;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ChangeShieldServerRpc(int toChange)
    {
        ApplyShieldChange(toChange);
    }
    private void ApplyShieldChange(int amount)
    {
        shield.Value = Mathf.Clamp(shield.Value + amount, 0, MaxShield);
        HasShield = shield.Value > 0;
    }

    private void HandlePlayerDeath()
    {
        if (IsServer && !IsDead)
        {
            isDead.Value = true;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void HandlePlayerDeathServerRpc()
    {
        HandlePlayerDeath();
    }



    private IEnumerator WaitForAlive()
    {
        yield return new WaitForSeconds(7.5f);
        ApplyHealthChange((int)maxHealth);
        isDead.Value = false;
    }
}
