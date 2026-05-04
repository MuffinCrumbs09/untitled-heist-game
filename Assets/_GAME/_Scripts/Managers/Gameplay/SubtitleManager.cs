using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages all subtitle display in the game (player + NPC).
/// Handles networking, proximity filtering, and NPC subtitle queuing.
/// </summary>
public class SubtitleManager : NetworkBehaviour
{
    #region Singleton
    public static SubtitleManager Instance { get; private set; }
    #endregion

    #region Inspector Settings

    [Header("Proximity Settings")]
    [Tooltip("Maximum distance at which player subtitles can be heard.")]
    [SerializeField] private float proximityRange = 15f;

    [Header("Display Settings")]
    [Tooltip("Default duration subtitles stay on screen.")]
    [SerializeField] private float defaultDisplayDuration = 3f;

    [Header("NPC Queue Settings")]
    [Tooltip("Maximum number of NPC subtitles that can be queued.")]
    [SerializeField] private int maxNpcQueueSize = 10;

    [Tooltip("How long a subtitle can wait before being discarded.")]
    [SerializeField] private float maxSubtitleAge = 8f;

    #endregion

    #region Private Fields
    private SubtitleUIManager uiManager;

    private Queue<SubtitleData> npcQueue = new();
    private Coroutine npcCoroutine;
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
        // Finds UI handler in scene
        uiManager = FindFirstObjectByType<SubtitleUIManager>();
    }
    #endregion

    #region Public API

    /// <summary>
    /// Displays a subtitle from the local player.
    /// </summary>
    public void ShowPlayerSubtitle(string message, float duration = -1f)
    {
        duration = duration < 0 ? defaultDisplayDuration : duration;

        ShowSubtitleServerRpc(GetLocalUsername(), message, SubtitleType.Player, duration);
    }

    /// <summary>
    /// Displays a subtitle from an NPC.
    /// </summary>
    public void ShowNPCSubtitle(string npcName, string message, float duration = -1f)
    {
        duration = duration < 0 ? defaultDisplayDuration : duration;

        ShowSubtitleServerRpc(npcName, message, SubtitleType.NPC, duration);
    }

    /// <summary>
    /// Clears all queued NPC subtitles.
    /// </summary>
    public void ClearNpcQueue()
    {
        npcQueue.Clear();

        if (npcCoroutine != null)
            StopCoroutine(npcCoroutine);

        npcCoroutine = null;
        isPlayingNpcSubtitle = false;
    }

    #endregion

    #region Networking

    /// <summary>
    /// Sends subtitle data to all clients.
    /// Routes to correct handling method based on type.
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void ShowSubtitleServerRpc(string speaker, string message, SubtitleType type, float duration, RpcParams rpc = default)
    {
        SubtitleData data = new SubtitleData
        {
            SenderClientId = rpc.Receive.SenderClientId,
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

    /// <summary>
    /// Adds NPC subtitles to a queue and plays them sequentially.
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastNpcSubtitleClientRpc(SubtitleData subtitleData)
    {
        if (npcQueue.Count >= maxNpcQueueSize)
            return;

        subtitleData.EnqueueTime = Time.time;
        npcQueue.Enqueue(subtitleData);

        if (!isPlayingNpcSubtitle)
            npcCoroutine = StartCoroutine(PlayNpcQueue());
    }

    /// <summary>
    /// Displays subtitles only if the local player is within range.
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastProximitySubtitleClientRpc(SubtitleData subtitleData)
    {
        if (NetworkManager.Singleton.LocalClient?.PlayerObject == null)
            return;

        GameObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        GameObject sender = GetPlayerByClientId(subtitleData.SenderClientId);

        if (sender == null)
            return;

        float distance = Vector3.Distance(localPlayer.transform.position, sender.transform.position);

        if (distance <= proximityRange)
            uiManager?.DisplaySubtitle(subtitleData.Username, subtitleData.Message, subtitleData.DisplayDuration);
    }

    #endregion

    #region NPC Queue System

    /// <summary>
    /// Plays NPC subtitles one at a time in order.
    /// </summary>
    private IEnumerator PlayNpcQueue()
    {
        isPlayingNpcSubtitle = true;

        while (npcQueue.Count > 0)
        {
            var next = npcQueue.Dequeue();

            float age = Time.time - next.EnqueueTime;

            // Skip expired subtitles
            if (age > maxSubtitleAge)
                continue;

            float remainingTime = next.DisplayDuration - age;

            if (remainingTime <= 0.5f)
                continue;

            uiManager?.DisplaySubtitle(next.Username, next.Message, remainingTime);

            yield return new WaitForSeconds(remainingTime);
        }

        isPlayingNpcSubtitle = false;
        npcCoroutine = null;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Finds a player GameObject using their client ID.
    /// </summary>
    private GameObject GetPlayerByClientId(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(client.PlayerObject.NetworkObjectId, out NetworkObject obj))
        {
            return obj.gameObject;
        }

        return null;
    }

    /// <summary>
    /// Gets the local player's username.
    /// </summary>
    private string GetLocalUsername()
    {
        ulong localID = NetworkManager.Singleton.LocalClientId;
        var manager = NetPlayerManager.Instance;

        foreach (var player in manager.playerData)
        {
            if (player.CLIENTID == localID)
                return player.USERNAME;
        }

        return "Player";
    }

    #endregion
}