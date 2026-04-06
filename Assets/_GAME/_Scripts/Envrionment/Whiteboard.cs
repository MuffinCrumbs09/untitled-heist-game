using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Whiteboard : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshPro displayText;

    private NetworkVariable<NetString> _displayedSerial = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        SetSerial(displayText.text); // Initialize with the default text from the inspector
        _displayedSerial.OnValueChanged += OnSerialChanged;

        // Apply the current value immediately in case we joined late
        UpdateText(_displayedSerial.Value.ToString());
    }

    public override void OnNetworkDespawn()
    {
        _displayedSerial.OnValueChanged -= OnSerialChanged;
    }

    /// <summary>Sets the serial segment shown on this whiteboard. Server only.</summary>
    public void SetSerial(string serial)
    {
        if (!IsServer) return;
        _displayedSerial.Value = serial;
    }

    private void OnSerialChanged(NetString previous, NetString current)
    {
        UpdateText(current);
    }

    private void UpdateText(string value)
    {
        if (displayText != null)
            displayText.text = value;
    }
}