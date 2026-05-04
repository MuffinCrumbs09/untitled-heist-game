using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Central manager for spawning waves of enemies based on network-synced configurations.
/// </summary>
public class EnemySpawner : NetworkBehaviour
{
    #region Variables

    [Header("Wave Configuration")]
    [SerializeField] 
    [Tooltip("List of wave data assets to be played in sequence.")]
    private List<WaveConfiguration> waveConfigurations = new List<WaveConfiguration>();

    [SerializeField] 
    [Tooltip("Rest period between waves in seconds.")]
    private float delayBetweenWaves = 10f;

    [Header("Spawn Points")]
    [SerializeField] 
    [Tooltip("All spawn points currently known to this spawner.")]
    private List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();

    [Header("Settings")]
    [SerializeField] 
    [Tooltip("Parent transform for spawned enemy objects.")]
    private Transform enemyContainer;

    [SerializeField] 
    [Tooltip("Should all alive enemies be destroyed when a wave timer ends?")]
    private bool clearEnemiesOnWaveEnd = false;

    private NetworkVariable<int> currentWaveIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> enemiesSpawnedThisWave = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> waveTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isWaveActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private List<NetworkObject> spawnedEnemies = new List<NetworkObject>();
    private Coroutine waveCoroutine;

    #endregion

    #region Properties

    public int CurrentWave => currentWaveIndex.Value;
    public int EnemiesSpawned => enemiesSpawnedThisWave.Value;
    public float WaveTimeRemaining => waveTimer.Value;
    public bool IsWaveActive => isWaveActive.Value;
    public int AliveEnemyCount => spawnedEnemies.Count;

    #endregion

    #region Unity & Network Lifecycle

    private void Awake()
    {
        if (enemyContainer == null) enemyContainer = new GameObject("Enemy Container").transform;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) StartWavesServerRpc();
    }

    #endregion

    #region Wave Sequencing

    /// <summary> Starts the wave progression logic on the server. </summary>
    [Rpc(SendTo.Server)]
    public void StartWavesServerRpc()
    {
        if (IsServer && waveCoroutine == null)
        {
            waveCoroutine = StartCoroutine(WaveSequence());
            spawnPoints.RemoveAll(sp => !sp.gameObject.activeInHierarchy);
        }
    }

    /// <summary> Coroutine that manages the transition between waves. </summary>
    private IEnumerator WaveSequence()
    {
        yield return new WaitForSeconds(2f);

        for (int i = 0; i < waveConfigurations.Count; i++)
        {
            currentWaveIndex.Value = i;
            WaveConfiguration config = waveConfigurations[i];

            yield return StartCoroutine(RunWave(config));

            if (!config.isEndlessWave)
            {
                SubtitleManager.Instance.ShowNPCSubtitle("Contractor", $"They are re-arming. Get ready {delayBetweenWaves} seconds.", 2.5f);
                if (clearEnemiesOnWaveEnd) ClearAllEnemies();
                yield return new WaitForSeconds(delayBetweenWaves);
            }
            else
            {
                SubtitleManager.Instance.ShowNPCSubtitle("Contractor", "This is it, they won't stop until your dead!", 2.5f);
                yield break;
            }
        }
    }

    /// <summary> Coroutine that handles spawning logic while a wave is active. </summary>
    private IEnumerator RunWave(WaveConfiguration config)
    {
        isWaveActive.Value = true;
        enemiesSpawnedThisWave.Value = 0;
        waveTimer.Value = config.waveDuration;
        float elapsedTime = 0f;

        while (config.isEndlessWave || elapsedTime < config.waveDuration)
        {
            waveTimer.Value = config.isEndlessWave ? 0f : config.waveDuration - elapsedTime;
            CleanupDestroyedEnemies();

            int availableSlots = config.maxSimultaneousEnemies - spawnedEnemies.Count;
            int enemiesToSpawnNow = Mathf.Min(config.GetEnemiesPerSpawn(GetConnectedPlayerCount()), availableSlots);

            if (enemiesToSpawnNow > 0)
            {
                SpawnEnemyGroup(config, enemiesToSpawnNow);
                enemiesSpawnedThisWave.Value += enemiesToSpawnNow;
            }

            yield return new WaitForSeconds(config.spawnInterval);
            if (!config.isEndlessWave) elapsedTime += config.spawnInterval;
        }
        isWaveActive.Value = false;
    }

    #endregion

    #region Spawning Logic

    /// <summary> Instantiates and spawns a group of enemies at unlocked spawn points. </summary>
    private void SpawnEnemyGroup(WaveConfiguration config, int groupSize)
    {
        var unlockedPoints = spawnPoints.Where(sp => sp != null && sp.IsUnlocked).ToList();
        if (unlockedPoints.Count == 0) return;

        for (int i = 0; i < groupSize; i++)
        {
            EnemySpawnPoint point = unlockedPoints[Random.Range(0, unlockedPoints.Count)];
            GameObject prefab = config.GetRandomEnemyPrefab();
            if (prefab == null) continue;

            GameObject instance = Instantiate(prefab, point.GetSpawnPosition(), point.GetSpawnRotation());
            if (instance.TryGetComponent(out NetworkObject netObj))
            {
                netObj.Spawn(true);
                spawnedEnemies.Add(netObj);
            }
            else Destroy(instance);
        }
    }

    /// <summary> Removes null references from the tracking list. </summary>
    private void CleanupDestroyedEnemies() => spawnedEnemies.RemoveAll(enemy => enemy == null);

    /// <summary> Returns the count of connected network clients. </summary>
    private int GetConnectedPlayerCount() => NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 1;

    #endregion

    #region Public Controls

    /// <summary> Adds a spawn point to the active pool. </summary>
    public void AddSpawnPoint(EnemySpawnPoint sp) { if (!spawnPoints.Contains(sp)) spawnPoints.Add(sp); }

    /// <summary> Removes a spawn point from the pool. </summary>
    public void RemoveSpawnPoint(EnemySpawnPoint sp) => spawnPoints.Remove(sp);

    /// <summary> Forces the spawner to skip to the next wave. </summary>
    [Rpc(SendTo.Server)]
    public void ForceNextWaveServerRpc()
    {
        if (IsServer && waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            ClearAllEnemies();
            waveCoroutine = StartCoroutine(WaveSequence());
        }
    }

    /// <summary> Despawns all enemies currently tracked by this spawner. </summary>
    private void ClearAllEnemies()
    {
        foreach (var enemy in spawnedEnemies) if (enemy != null) enemy.Despawn(true);
        spawnedEnemies.Clear();
    }

    public override void OnDestroy() { if (IsServer) ClearAllEnemies(); }

    #endregion
}