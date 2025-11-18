using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private string filePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;

        filePath = Application.persistentDataPath + "/PlayerData.json";
    }

    public void SaveGame(PlayerStats stats)
    {
        string json = JsonUtility.ToJson(stats, true);

        File.WriteAllText(filePath, json);
    }

    public PlayerStats LoadGame()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);

            PlayerStats stats = JsonUtility.FromJson<PlayerStats>(json);
            return stats;
        }
        else
            return new PlayerStats();
    }
}