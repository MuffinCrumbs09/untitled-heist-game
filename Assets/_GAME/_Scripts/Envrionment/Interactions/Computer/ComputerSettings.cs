using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles the visual properties and power state of a computer.
/// </summary>
public class ComputerSettings : NetworkBehaviour
{
    [Header("Power Settings")]
    public NetworkVariable<bool> IsOn = new(false);
    [SerializeField] private bool ChangeMats = true;

    [Header("Visual References")]
    [SerializeField] private Material[] OnMats;
    [SerializeField] private Renderer render;

    public override void OnNetworkSpawn()
    {
        IsOn.OnValueChanged += OnPowerStateChanged;
        
        // Ensure visual state matches current network value for late-joiners
        if (IsOn.Value) OnPowerStateChanged(false, true);
    }

    /// <summary>
    /// Updates the computer's screen materials when the power state changes.
    /// </summary>
    private void OnPowerStateChanged(bool previousValue, bool newValue)
    {
        if (newValue && ChangeMats && render != null)
        {
            Material[] mats = render.materials;
            for (int i = 1; i < mats.Length; i++)
            {
                mats[i] = OnMats[Random.Range(0, OnMats.Length)];
            }
            render.materials = mats;
        }
    }

    [Rpc(SendTo.Server)]
    public void SetIsOnRpc(bool value) => IsOn.Value = value;
}