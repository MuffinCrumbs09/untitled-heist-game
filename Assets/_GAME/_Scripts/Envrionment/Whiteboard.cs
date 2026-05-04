using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Networked whiteboard that displays a synchronised text string on a TextMeshPro object.
/// The displayed text is stored as a server-authoritative NetworkVariable so all clients always see the same value, including those who join late.
/// </summary>
public class Whiteboard : NetworkBehaviour
{
    [Tooltip("The TextMeshPro component in the scene that renders the whiteboard text.\nAssign the text object on the whiteboard mesh in the Inspector.")]
    [SerializeField] private TextMeshPro displayText;

    // The currently displayed string, synchronised from server to all clients.
    private NetworkVariable<NetString> _displayedSerial = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    #region Unity LifeCycle
    /// <summary>
    /// Called on all clients when the object is spawned on the network.
    /// Initialises the serial with the default text set in the Inspector, then subscribes to future changes. 
    /// Also applies the current value immediately to handle clients that join after the value has already been set.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // Use whatever text is already in the TextMeshPro field as the starting value.
        SetSerial(displayText.text);

        _displayedSerial.OnValueChanged += OnSerialChanged;

        // Apply the synced value immediately in case this client joined late
        // and missed the initial SetSerial call.
        UpdateText(_displayedSerial.Value.ToString());
    }

    /// <summary>
    /// Unsubscribes from the NetworkVariable change event when this object is despawned.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        _displayedSerial.OnValueChanged -= OnSerialChanged;
    }
    #endregion

    /// <summary>
    /// Updates the text shown on this whiteboard. Server only.
    /// </summary>
    /// <param name="serial">The string to display on the whiteboard.</param>
    public void SetSerial(string serial)
    {
        if (!IsServer) return;
        _displayedSerial.Value = serial;
    }

    // Callback fired on all clients when _displayedSerial changes on the server.
    private void OnSerialChanged(NetString previous, NetString current)
    {
        UpdateText(current);
    }

    // Applies the given string to the TextMeshPro component if it exists.
    private void UpdateText(string value)
    {
        if (displayText != null)
            displayText.text = value;
    }
}