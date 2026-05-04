using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Networked trigger that plays a one-shot NPC subtitle when a player walks into its collider.
/// Once triggered, it cannot fire again — the isTriggered flag is stored as a server-authoritative
/// NetworkVariable so late-joining clients also respect the already-triggered state.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class SubtitleTrigger : NetworkBehaviour
{
    [Tooltip("The name displayed in the subtitle UI as the speaker.")]
    [SerializeField] private string speakerName;

    [Tooltip("The dialogue line shown in the subtitle UI.")]
    [SerializeField, TextArea] private string speech;

    // Tracks whether this trigger has already fired. Server-authoritative and replicated
    // to all clients so the subtitle never plays twice, even after scene reload or late join.
    private NetworkVariable<bool> isTriggered = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Convenience property to read the current triggered state.
    private bool IsTriggered => isTriggered.Value;

    // Shorthand accessor for the singleton SubtitleManager used to display subtitles.
    private SubtitleManager Subtitle => SubtitleManager.Instance;

    /// <summary>
    /// Fires when a collider enters the trigger zone.
    /// Once triggered, returns early
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Do nothing if this trigger has already been used.
        if (IsTriggered) return;

        // Only the local player who owns their PlayerObject should trigger this.
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            // Asks the SubtitleManager to send a one-shot subtitle to all clients
            Subtitle.ShowNPCSubtitle(speakerName, speech, 6.5f);

            // Inform the server so it can mark this trigger as used for all clients.
            TriggerSubtitleServerRpc();
        }
    }

    /// <summary>
    /// Server RPC that marks this trigger as permanently used.
    /// The double-check on IsTriggered guards against rare condition
    /// where two clients call this before the NetworkVariable updates.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TriggerSubtitleServerRpc()
    {
        if (IsTriggered) return;

        isTriggered.Value = true;
    }
}