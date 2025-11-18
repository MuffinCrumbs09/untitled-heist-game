using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Wave Configuration")]
    [SerializeField] private List<WaveConfiguration> waveConfigurations = new List<WaveConfiguration>();
    [SerializeField] private float delayBetweenWaves = 10f;

    [Header("Spawn Points")]
    [SerializeField] private List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();

    [Header("Settings")]
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private bool clearEnemiesOnWaveEnd = false;

    private NetworkVariable<int> currentWaveIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> enemiesSpawnedThisWave = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> waveTimer = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isWaveActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private List<NetworkObject> spawnedEnemies = new List<NetworkObject>();
    private Coroutine waveCoroutine;

    public int CurrentWave => currentWaveIndex.Value;
    public int EnemiesSpawned => enemiesSpawnedThisWave.Value;
    public float WaveTimeRemaining => waveTimer.Value;
    public bool IsWaveActive => isWaveActive.Value;
    public int AliveEnemyCount => spawnedEnemies.Count;

    private void Awake()
    {
        if (enemyContainer == null)
        {
            GameObject container = new GameObject("Enemy Container");
            enemyContainer = container.transform;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartWavesServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartWavesServerRpc()
    {
        if (IsServer && waveCoroutine == null)
        {
            waveCoroutine = StartCoroutine(WaveSequence());

            for(int i = 0; i < spawnPoints.Count; i++)
            {
                if(!spawnPoints[i].gameObject.activeInHierarchy)
                    spawnPoints.Remove(spawnPoints[i]);
            }
        }
    }

    private IEnumerator WaveSequence()
    {
        yield return new WaitForSeconds(2f);

        for (int i = 0; i < waveConfigurations.Count; i++)
        {
            currentWaveIndex.Value = i;
            WaveConfiguration config = waveConfigurations[i];

            Debug.Log($"Starting Wave {config.waveNumber}");
            AnnounceWaveStartClientRpc(config.waveNumber);

            yield return StartCoroutine(RunWave(config));

            if (!config.isEndlessWave)
            {
                Debug.Log($"Wave {config.waveNumber} completed. Next wave in {delayBetweenWaves} seconds.");

                SubtitleManager.Instance.ShowNPCSubtitle("Contractor", $"They are re-arming. Get ready {delayBetweenWaves} seconds.", 6.5f);
                
                if (clearEnemiesOnWaveEnd)
                {
                    ClearAllEnemies();
                }
                
                yield return new WaitForSeconds(delayBetweenWaves);
            }
            else
            {
                SubtitleManager.Instance.ShowNPCSubtitle("Contractor", "This is it, they won't stop until your dead!", 6.5f);
                Debug.Log("Endless wave started - no more waves will begin.");
                yield break;
            }
        }

        Debug.Log("All waves completed!");
    }

    private IEnumerator RunWave(WaveConfiguration config)
    {
        isWaveActive.Value = true;
        enemiesSpawnedThisWave.Value = 0;
        waveTimer.Value = config.waveDuration;

        int playerCount = GetConnectedPlayerCount();
        int enemiesPerSpawn = config.GetEnemiesPerSpawn(playerCount);

        float elapsedTime = 0f;

        while (config.isEndlessWave || elapsedTime < config.waveDuration)
        {
            waveTimer.Value = config.isEndlessWave ? 0f : config.waveDuration - elapsedTime;

            CleanupDestroyedEnemies();

            int availableSlots = config.maxSimultaneousEnemies - spawnedEnemies.Count;
            int enemiesToSpawnNow = Mathf.Min(enemiesPerSpawn, availableSlots);

            if (enemiesToSpawnNow > 0)
            {
                SpawnEnemyGroup(config, enemiesToSpawnNow);
                enemiesSpawnedThisWave.Value += enemiesToSpawnNow;
            }

            yield return new WaitForSeconds(config.spawnInterval);
            
            if (!config.isEndlessWave)
            {
                elapsedTime += config.spawnInterval;
            }
        }

        isWaveActive.Value = false;
        Debug.Log($"Wave {config.waveNumber} time completed. {spawnedEnemies.Count} enemies still alive.");
    }

    private void SpawnEnemyGroup(WaveConfiguration config, int groupSize)
    {
        List<EnemySpawnPoint> unlockedSpawnPoints = spawnPoints.Where(sp => sp != null && sp.IsUnlocked).ToList();

        if (unlockedSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No unlocked spawn points available!");
            return;
        }

        for (int i = 0; i < groupSize; i++)
        {
            EnemySpawnPoint selectedSpawnPoint = unlockedSpawnPoints[Random.Range(0, unlockedSpawnPoints.Count)];
            GameObject enemyPrefab = config.GetRandomEnemyPrefab();

            if (enemyPrefab == null)
            {
                Debug.LogError("Enemy prefab is null!");
                continue;
            }

            Vector3 spawnPosition = selectedSpawnPoint.GetSpawnPosition();
            Quaternion spawnRotation = selectedSpawnPoint.GetSpawnRotation();

            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, spawnRotation, enemyContainer);
            NetworkObject networkObject = enemyInstance.GetComponent<NetworkObject>();

            if (networkObject != null)
            {
                networkObject.Spawn(true);
                spawnedEnemies.Add(networkObject);
            }
            else
            {
                Debug.LogError($"Enemy prefab {enemyPrefab.name} does not have a NetworkObject component!");
                Destroy(enemyInstance);
            }
        }
    }

    private void CleanupDestroyedEnemies()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
    }

    private int GetConnectedPlayerCount()
    {
        if (NetworkManager.Singleton != null)
        {
            return NetworkManager.Singleton.ConnectedClientsList.Count;
        }
        return 1;
    }

    public void AddSpawnPoint(EnemySpawnPoint spawnPoint)
    {
        if (!spawnPoints.Contains(spawnPoint))
        {
            spawnPoints.Add(spawnPoint);
        }
    }

    public void RemoveSpawnPoint(EnemySpawnPoint spawnPoint)
    {
        spawnPoints.Remove(spawnPoint);
    }

    [ClientRpc]
    private void AnnounceWaveStartClientRpc(int waveNumber)
    {
        Debug.Log($"[Client] Wave {waveNumber} has started!");
    }

    [ServerRpc(RequireOwnership = false)]
    public void ForceNextWaveServerRpc()
    {
        if (IsServer && waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            ClearAllEnemies();
            waveCoroutine = StartCoroutine(WaveSequence());
        }
    }

    private void ClearAllEnemies()
    {
        foreach (NetworkObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                enemy.Despawn(true);
            }
        }
        spawnedEnemies.Clear();
    }

    public override void OnDestroy()
    {
        if (IsServer)
        {
            ClearAllEnemies();
        }
    }
}
