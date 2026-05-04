using UnityEngine;

/// <summary>
/// ScriptableObject that defines the settings for a single wave of enemies.
/// Create new wave via: Assets > Create > Enemy Spawning > Wave Configuration
/// </summary>
[CreateAssetMenu(fileName = "New Wave Config", menuName = "Enemy Spawning/Wave Configuration")]
public class WaveConfiguration : ScriptableObject
{
    #region Settings

    [Header("Wave Settings")]
    [Tooltip("The ID or sequence number of this wave.")]
    public int waveNumber;

    [Tooltip("How long the wave lasts in seconds.")]
    public float waveDuration = 120f;

    [Tooltip("If true, this wave will never end and no subsequent waves will trigger.")]
    public bool isEndlessWave = false;

    [Header("Enemy Spawning")]
    [Tooltip("Pool of enemy prefabs that can be spawned during this wave.")]
    public GameObject[] enemyPrefabs;
    
    [Tooltip("Base number of enemies to spawn per spawn event.")]
    public int baseEnemiesPerSpawn = 1;
    
    [Tooltip("Increases spawn count based on the number of players in the session.")]
    public int additionalEnemiesPerPlayerPerSpawn = 1;
    
    [Tooltip("Seconds to wait between each spawn event.")]
    public float spawnInterval = 5f;
    
    [Tooltip("Max number of enemies from this wave that can exist at once.")]
    public int maxSimultaneousEnemies = 20;

    [Tooltip("Hard cap on enemies spawned per interval, regardless of player count.")]
    public int maxEnemiesPerSpawn = 10;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Calculates how many enemies to spawn based on player count and caps.
    /// </summary>
    public int GetEnemiesPerSpawn(int playerCount)
    {
        int enemiesPerSpawn = baseEnemiesPerSpawn + (additionalEnemiesPerPlayerPerSpawn * (playerCount - 1));
        return Mathf.Min(enemiesPerSpawn, maxEnemiesPerSpawn);
    }

    /// <summary>
    /// Returns a random prefab from the enemy pool.
    /// </summary>
    public GameObject GetRandomEnemyPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
        return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    }

    #endregion
}