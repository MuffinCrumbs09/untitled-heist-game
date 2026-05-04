using System.IO;
using UnityEngine;

namespace Stats
{
    /// <summary>
    /// Handles saving and loading player stats to a local JSON file.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        #region Singleton
        public static SaveManager Instance;
        #endregion

        #region Private Fields
        private string filePath;
        #endregion

        #region Unity Events
        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this);

            Instance = this;

            // Path where save file is stored
            filePath = Application.persistentDataPath + "/PlayerData.json";
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Saves player stats as JSON.
        /// </summary>
        public void SaveGame(PlayerStats stats)
        {
            string json = JsonUtility.ToJson(stats, true);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads player stats, or creates new if none exist.
        /// </summary>
        public PlayerStats LoadGame()
        {
            if (!File.Exists(filePath))
                return new PlayerStats();

            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<PlayerStats>(json);
        }

        #endregion
    }
}