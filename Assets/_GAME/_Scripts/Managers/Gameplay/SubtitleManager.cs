using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SubtitleManager : NetworkBehaviour
{
    #region Singleton
    public static SubtitleManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [Header("Proximity Settings")]
    [SerializeField] private float ProximityRange = 15f;

    [Header("Display Settings")]
    [SerializeField] private float DefaultDisplayDuration = 3f;

    [Header("NPC Queue Settings")]
    [SerializeField] private int MaxNpcQueueSize = 10;
    #endregion

    #region Private Fields
    private SubtitleUIManager uiManager;
    private Queue<SubtitleData> npcQueue = new Queue<SubtitleData>();
    private bool isPlayingNpcSubtitle = false;
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

    #region Public Methods
    public void ShowPlayerSubtitle(string message, float duration = -1f)
    {
        duration = duration < 0 ? DefaultDisplayDuration : duration;
        string username = GetLocalUsername();

        ShowSubtitleServerRpc(username, message, SubtitleType.Player, duration);
    }

    public void ShowNPCSubtitle(string npcName, string message, float duration = -1f)
    {
        duration = duration < 0 ? DefaultDisplayDuration : duration;

        ShowSubtitleServerRpc(npcName, message, SubtitleType.NPC, duration);
    }

    public void ClearNpcQueue()
    {
        npcQueue.Clear();
        StopCoroutine(nameof(PlayNpcQueue));
        isPlayingNpcSubtitle = false;
    }
    #endregion

    #region RPCs
    [Rpc(SendTo.ClientsAndHost)]
    private void ShowSubtitleServerRpc(string speaker, string message, SubtitleType type, float duration, RpcParams rpc = default)
    {
        ulong senderClientId = rpc.Receive.SenderClientId;

        SubtitleData data = new SubtitleData
        {
            SenderClientId = senderClientId,
            Username = speaker,
            Message = message,
            Type = type,
            DisplayDuration = duration
        };

        if (type == SubtitleType.NPC)
            BroadcastNpcSubtitleClientRpc(data);
        else
            BroadcastProximitySubtitleClientRpc(data);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastNpcSubtitleClientRpc(SubtitleData subtitleData)
    {
        if (npcQueue.Count >= MaxNpcQueueSize)
            return;

        npcQueue.Enqueue(subtitleData);

        if (!isPlayingNpcSubtitle)
            StartCoroutine(nameof(PlayNpcQueue));
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastProximitySubtitleClientRpc(SubtitleData subtitleData)
    {
        if (NetworkManager.Singleton.LocalClient?.PlayerObject == null)
            return;

        GameObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        GameObject senderPlayer = GetPlayerByClientId(subtitleData.SenderClientId);

        if (senderPlayer == null)
            return;

        float distance = Vector3.Distance(localPlayer.transform.position, senderPlayer.transform.position);

        if (distance <= ProximityRange)
            uiManager?.DisplaySubtitle(subtitleData.Username, subtitleData.Message, subtitleData.DisplayDuration);
    }
    #endregion

    #region NPC Queue
    private IEnumerator PlayNpcQueue()
    {
        isPlayingNpcSubtitle = true;

        while (npcQueue.Count > 0)
        {
            SubtitleData next = npcQueue.Dequeue();
            uiManager?.DisplaySubtitle(next.Username, next.Message, next.DisplayDuration);
            yield return new WaitForSeconds(next.DisplayDuration);
        }

        isPlayingNpcSubtitle = false;
    }
    #endregion

    #region Private Methods
    private GameObject GetPlayerByClientId(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                client.PlayerObject.NetworkObjectId, out NetworkObject networkObject))
        {
            return networkObject.gameObject;
        }
        return null;
    }

    private string GetLocalUsername()
    {
        ulong localID = NetworkManager.Singleton.LocalClientId;
        NetPlayerManager manager = NetPlayerManager.Instance;

        for (int i = 0; i < manager.playerData.Count; i++)
        {
            if (manager.playerData[i].CLIENTID.Equals(localID))
                return manager.playerData[i].USERNAME;
        }

        return "Player";
    }
    #endregion
}