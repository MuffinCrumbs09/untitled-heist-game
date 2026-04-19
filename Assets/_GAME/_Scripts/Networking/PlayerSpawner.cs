using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject player;

    private bool _playersSpawned;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
    }

    private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsHost || sceneName != "MicroBank" || _playersSpawned)
            return;

        if (NetworkManager.Singleton.ConnectedClients.Count < NetPlayerManager.Instance.playerData.Count)
            return;

        PlayerSpawnPoint spawnPointsHolder = Object.FindFirstObjectByType<PlayerSpawnPoint>();
        List<Transform> availableSpawnPoints = new List<Transform>(spawnPointsHolder.Points);

        _playersSpawned = true;

        foreach (ulong id in clientsCompleted)
        {
            if (availableSpawnPoints.Count == 0)
                availableSpawnPoints = new List<Transform>(spawnPointsHolder.Points);

            int randomIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnTransform = availableSpawnPoints[randomIndex];
            availableSpawnPoints.RemoveAt(randomIndex);

            GameObject spawnedPlayer = Instantiate(player, spawnTransform.position, spawnTransform.rotation);
            NetworkObject netObj = spawnedPlayer.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(id, true);
            
            TeleportClientRpc(spawnTransform.position, spawnTransform.rotation);
        }
    }

    [Rpc(SendTo.NotServer)]
    private void TeleportClientRpc(Vector3 position, Quaternion rotation)
    {
        if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() is NetworkObject playerObj)
        {
            playerObj.transform.SetPositionAndRotation(position, rotation);
        }
    }
}