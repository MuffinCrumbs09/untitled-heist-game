using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class SubtitleTrigger : NetworkBehaviour
{
    [Header("Subtitle Settings")]
    [SerializeField] private string speakerName;
    [SerializeField, TextArea] private string speech;

    private NetworkVariable<bool> isTriggered = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool IsTriggered => isTriggered.Value;
    private SubtitleManager Subtitle => SubtitleManager.Instance;

    private void OnTriggerEnter(Collider other)
    {
        if (IsTriggered) return;

        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            Subtitle.ShowNPCSubtitle(speakerName, speech, 6.5f);
            TriggerSubtitleServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerSubtitleServerRpc()
    {
        if (IsTriggered) return;

        isTriggered.Value = true;
    }
}