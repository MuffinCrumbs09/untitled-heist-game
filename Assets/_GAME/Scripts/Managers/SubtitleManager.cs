using System;
using Unity.Netcode;
using UnityEngine;

public class SubtitleManager : NetworkBehaviour
{
    #region Variables
    public static SubtitleManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float ProximityRange = 15f;
    [SerializeField] private float DefaultDisplayDuration = 3f;

    private SubtitleUIManager uiManager;
    #endregion

    #region Unity Events
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        uiManager = FindFirstObjectByType<SubtitleUIManager>();
    }
    #endregion

    public void ShowPlayerSubtitle(string message, float duration = -1f)
    {
        duration = duration < 0 ? DefaultDisplayDuration : duration;
        string username = GetLocalUsername();

        ShowSubtitleServerRpc(username, message, false, duration);
    }

    public void ShowNPCSubtitle(string npcName, string message, float duration = -1f)
    {
        duration = duration < 0 ? DefaultDisplayDuration : duration;

        ShowSubtitleServerRpc(npcName, message, true, duration);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShowSubtitleServerRpc(string speaker, string message, bool isGlobal, float duration, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        SubtitleData data = new SubtitleData
        {
            SenderClientId = senderClientId,
            Username = speaker,
            Message = message,
            IsGlobal = isGlobal,
            DisplayDuration = duration
        };

        if (isGlobal)
            BroadcastSubtitleClientRpc(data);
        else
            BroadcastProximitySubtitleClientRpc(data);
    }

    [ClientRpc]
    private void BroadcastSubtitleClientRpc(SubtitleData subtitleData)
    {
        uiManager?.DisplaySubtitle(subtitleData.Username.ToString(), subtitleData.Message.ToString(), subtitleData.DisplayDuration);
    }

    [ClientRpc]
    private void BroadcastProximitySubtitleClientRpc(SubtitleData subtitleData)
    {
        if (NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
            return;

        GameObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        GameObject senderPlayer = GetPlayerByClientId(subtitleData.SenderClientId);

        if (senderPlayer == null)
            return;

        float distance = Vector3.Distance(localPlayer.transform.position, senderPlayer.transform.position);

        if (distance <= ProximityRange)
        {
            uiManager?.DisplaySubtitle(subtitleData.Username.ToString(), subtitleData.Message.ToString(), subtitleData.DisplayDuration);
        }
    }

    private GameObject GetPlayerByClientId(ulong clientId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.NetworkObjectId,
            out NetworkObject networkObject))
        {
            return networkObject.gameObject;
        }
        return null;
    }

    private string GetLocalUsername()
    {
        if (NetPlayerManager.Instance != null && NetPlayerManager.Instance.usernames.Count > 0)
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;

            if (localClientId < (ulong)NetPlayerManager.Instance.usernames.Count)
            {
                return NetPlayerManager.Instance.usernames[(int)localClientId].ToString();
            }
        }

        return "Player";
    }
}
