using UnityEngine;
using Unity.Netcode;

public class PlayerHealthController : Health
{
    [Header("Settings - Shield")]
    public int MaxShield;
    public float ShieldRegenTime;
    public int ShieldRegenAmount;

    private float time;

    public bool HasShield { private set; get; } = true;
    private NetworkVariable<float> shield = new(
        writePerm: NetworkVariableWritePermission.Server
    );

    private void Start()
    {
        shield.Value = MaxShield;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (shield.Value < MaxShield)
            RegenerateShield();
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
        if (time >= ShieldRegenTime)
        {
            ChangeShieldServerRpc(ShieldRegenAmount);
            time = 0;
        }

        time++;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeShieldServerRpc(int toChange)
    {
        ApplyShieldChange(toChange);
    }
    private void ApplyShieldChange(int amount)
    {
        shield.Value = Mathf.Clamp(shield.Value + amount, 0, MaxShield);
        HasShield = shield.Value > 0;
    }
}
