using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerTargetingManager : MonoBehaviour
{
    public static PlayerTargetingManager Instance { get; private set; }

    private Dictionary<ulong, Transform> clientIdToTargetMap = new();   // Maps client IDs to their corresponding player transforms
    private Dictionary<GameObject, ulong> enemyToTargetMap = new();   // Maps enemy game objects to their current target transforms
    private Dictionary<ulong, int> playerEnemyCountMap = new(); // Maps client IDs to the number of enemies currently targeting them

    private void Awake()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (NetPlayerManager.Instance.playerData.Count > clientIdToTargetMap.Count)
        {
            MapPlayersToDictionary();
        }
    }

    public Dictionary<GameObject, ulong> GetEnemyToTargetMap()
    {
        return enemyToTargetMap;
    }

    public void RegisterTarget(ulong clientId, GameObject enemyObj)
    {
        if (enemyToTargetMap.ContainsKey(enemyObj))
        {
            UnregisterTarget(enemyObj);
        }

        enemyToTargetMap[enemyObj] = clientId;
        if (!playerEnemyCountMap.ContainsKey(clientId))
            playerEnemyCountMap[clientId] = 0;
        playerEnemyCountMap[clientId] += 1;
    }

    public void UnregisterTarget(GameObject enemyObj)
    {
        if (!enemyToTargetMap.ContainsKey(enemyObj)) return;

        ulong oldPlayer = enemyToTargetMap[enemyObj];
        enemyToTargetMap.Remove(enemyObj);
        playerEnemyCountMap[oldPlayer] -= 1;
    }
    
    public int GetEnemyCountForPlayer(ulong clientId)
    {
        return playerEnemyCountMap.ContainsKey(clientId) ? playerEnemyCountMap[clientId] : -1;
    }

    public ulong[] GetAllClients()
    {
        ulong[] clients = new ulong[clientIdToTargetMap.Count];
        clientIdToTargetMap.Keys.CopyTo(clients, 0);
        return clients;
    }

    public int GetThreshold()
    {
        int extra = clientIdToTargetMap.Count - 1;
        return 10 + (2 * extra);
    }

    public ulong GetCurrentTarget(GameObject enemyObj)
    {
        return enemyToTargetMap.ContainsKey(enemyObj) ? enemyToTargetMap[enemyObj] : ulong.MaxValue;
    }

    public GameObject GetCurrentPlayerObject(ulong clientId)
    {
        if (!clientIdToTargetMap.ContainsKey(clientId)) return null;
        return clientIdToTargetMap[clientId].gameObject;
    }

    private void MapPlayersToDictionary()
    {
        clientIdToTargetMap.Clear();
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");

        foreach (var playerData in NetPlayerManager.Instance.playerData)
        {
            GameObject playerObject = null;

            foreach (var obj in playerObjects)
            {
                if (obj.GetComponent<NetworkObject>().OwnerClientId == playerData.CLIENTID)
                {
                    playerObject = obj;
                    break;
                }
            }

            if (playerObject != null)
            {
                clientIdToTargetMap[playerData.CLIENTID] = playerObject.transform;
            }
        }
    }
}
