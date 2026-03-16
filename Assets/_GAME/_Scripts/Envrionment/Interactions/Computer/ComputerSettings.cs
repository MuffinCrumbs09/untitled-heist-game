using System;
using Unity.Netcode;
using UnityEngine;

public class ComputerSettings : NetworkBehaviour
{
    [Header("Settings")]
    public NetworkVariable<bool> IsOn = new(false);
    [SerializeField] private bool ChangeMats = true;

    [Header("References")]
    [SerializeField] private Material[] OnMats;
    [SerializeField] private Renderer render;

    public override void OnNetworkSpawn()
    {
        IsOn.OnValueChanged += OnIsOnChanged;
        // Apply initial state for late-joiners or pre-set values
        if (IsOn.Value && ChangeMats)
            OnIsOnChanged(false, true);
    }

    public override void OnNetworkDespawn()
    {
        IsOn.OnValueChanged -= OnIsOnChanged;
    }


    [Rpc(SendTo.Server)]
    public void SetIsOnRpc(bool value)
    {
        IsOn.Value = value;
    }

    private void OnIsOnChanged(bool previousValue, bool newValue)
    {
        if (newValue && ChangeMats)
        {
            Material[] mats = render.materials;
            for (int i = 1; i < mats.Length; i++)
            {
                mats[i] = OnMats[UnityEngine.Random.Range(0, OnMats.Length)];
            }
            render.materials = mats;
        }
    }
}

