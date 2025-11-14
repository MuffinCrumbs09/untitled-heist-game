using UnityEngine;

[CreateAssetMenu(fileName = "New Wave Config", menuName = "Enemy Spawning/Wave Configuration")]
public class WaveConfiguration : ScriptableObject
{
    [Header("Wave Settings")]
    public int waveNumber;
    public float waveDuration = 120f;
    public bool isEndlessWave = false;

    [Header("Enemy Spawning")]
    public GameObject[] enemyPrefabs;
    
    [Tooltip("Base number of enemies to spawn per spawn event")]
    public int baseEnemiesPerSpawn = 1;
    
    [Tooltip("Additional enemies per spawn for each player beyond the first")]
    public int additionalEnemiesPerPlayerPerSpawn = 1;
    
    [Tooltip("How often to spawn a group of enemies (in seconds)")]
    public float spawnInterval = 5f;
    
    [Tooltip("Maximum enemies alive at once")]
    public int maxSimultaneousEnemies = 20;

    [Tooltip("Maximum enemies per spawn regardless of player count")]
    public int maxEnemiesPerSpawn = 10;

    public int GetEnemiesPerSpawn(int playerCount)
    {
        int enemiesPerSpawn = baseEnemiesPerSpawn + (additionalEnemiesPerPlayerPerSpawn * (playerCount - 1));
        return Mathf.Min(enemiesPerSpawn, maxEnemiesPerSpawn);
    }

    public GameObject GetRandomEnemyPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("No enemy prefabs assigned to wave configuration!");
            return null;
        }

        return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    }
}
