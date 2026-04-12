using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyTargetingBrain : MonoBehaviour
{
    public ulong currentTargetClientId { get; private set; } = ulong.MaxValue; // Start with an invalid client ID

    private ulong lastShotByPlayerId = ulong.MaxValue;
    private float lastShotTime;

    private PlayerTargetingManager manager => PlayerTargetingManager.Instance;

    private string TargetingTag => "[EnemyTargetingBrain]".Color(Color.skyBlue);


    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) this.enabled = false; // Only the server should run this logic
    }

    public void PickInitalTarget()
    {
        int lowestEnemyCount = int.MaxValue;
        ulong selectedClientId = ulong.MaxValue;

        foreach (var playerData in NetPlayerManager.Instance.playerData)
        {
            int enemyCount = PlayerTargetingManager.Instance.GetEnemyCountForPlayer(playerData.CLIENTID);
            if (enemyCount < lowestEnemyCount)
            {
                lowestEnemyCount = enemyCount;
                selectedClientId = playerData.CLIENTID;
            }
        }

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Enemy,
        $"{TargetingTag} : Initial Target Picked - Player {selectedClientId} with {lowestEnemyCount} enemies targeting them.",
         this);
#endif
        currentTargetClientId = selectedClientId;
        PlayerTargetingManager.Instance.RegisterTarget(currentTargetClientId, gameObject);
    }

    public bool TryRedistirubuteTarget(float distanceToTarget)
    {
        if (distanceToTarget >= 50f) return false;

        int threshold = PlayerTargetingManager.Instance.GetThreshold();

        if (manager.GetEnemyCountForPlayer(currentTargetClientId) < threshold) return false;

        List<ulong> underFullPlayers = new();
        foreach (ulong player in manager.GetAllClients())
        {
            if (manager.GetEnemyCountForPlayer(player) < threshold)
            {
                underFullPlayers.Add(player);
            }
        }

        if (underFullPlayers.Count == 0) return false;

        ulong newTarget = PickRandomFrom(underFullPlayers);
        SetTarget(newTarget);
        return true;
    }

    public bool TryInteruptTarget(float distanceToTarget, bool isShooting)
    {
        // Shot by someone else recently
        if (lastShotByPlayerId != ulong.MaxValue && lastShotByPlayerId != currentTargetClientId && Time.time - lastShotTime < 5f)
        {
            SetTarget(lastShotByPlayerId);
            lastShotByPlayerId = ulong.MaxValue;
            return true;
        }

        // Too close to target or isnt shooting
        if (distanceToTarget < 25f || !isShooting) return false;

        ulong newTarget = PickRandomPlayerExcluding(currentTargetClientId);
        if (newTarget != ulong.MaxValue)
        {
            SetTarget(newTarget);
            return true;
        }

        return false;
    }

    public void OnDamagedByPlayer(ulong playerId)
    {
        lastShotByPlayerId = playerId;
        lastShotTime = Time.time;
    }

    public GameObject GetCurrentTargetGameobject()
    {
        ulong id = manager.GetCurrentTarget(gameObject);
        return manager.GetCurrentPlayerObject(id);
    }

    private ulong PickRandomPlayerExcluding(ulong current)
    {
        ulong[] allPlayers = manager.GetAllClients();
        List<ulong> validPlayers = new();

        foreach (ulong player in allPlayers)
        {
            if (player != current) validPlayers.Add(player);
        }

        if (validPlayers.Count == 0) return ulong.MaxValue;

        return validPlayers[UnityEngine.Random.Range(0, validPlayers.Count)];
    }

    private void SetTarget(ulong newTarget)
    {
        manager.RegisterTarget(newTarget, gameObject);
        currentTargetClientId = newTarget;
    }

    private ulong PickRandomFrom(List<ulong> underFullPlayers)
    {
        if (underFullPlayers.Count == 0) return ulong.MaxValue;
        return underFullPlayers[UnityEngine.Random.Range(0, underFullPlayers.Count)];
    }
}
