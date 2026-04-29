using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    private bool playersSpawned = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsHost)
            return;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(
        string sceneName,
        LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        if (!IsHost || sceneName != "MicroBank" || playersSpawned)
            return;

        Debug.Log($"[Spawner] Scene loaded: {sceneName}");

        PlayerSpawnPoint spawnPointsHolder = Object.FindFirstObjectByType<PlayerSpawnPoint>();

        if (spawnPointsHolder == null)
        {
            Debug.LogError("[Spawner] No PlayerSpawnPoint found in scene!");
            return;
        }

        List<Transform> availableSpawnPoints = new List<Transform>(spawnPointsHolder.Points);

        if (availableSpawnPoints.Count == 0)
        {
            Debug.LogError("[Spawner] No spawn points assigned!");
            return;
        }

        playersSpawned = true;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (availableSpawnPoints.Count == 0)
                availableSpawnPoints = new List<Transform>(spawnPointsHolder.Points);

            int randomIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnPoint = availableSpawnPoints[randomIndex];
            availableSpawnPoints.RemoveAt(randomIndex);

            GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("[Spawner] Player prefab is missing NetworkObject!");
                return;
            }

            // Spawn first, then tell the client to warp to the position
            netObj.SpawnAsPlayerObject(clientId, true);
        }
    }
}