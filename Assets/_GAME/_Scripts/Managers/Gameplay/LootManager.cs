using UnityEngine;

/// <summary>
/// Handles tracking of collected loot in the game.
/// Implements a simple singleton for global access.
/// Currently unused as bags have not been implemented.
/// </summary>
public class LootManager : MonoBehaviour
{
    public static LootManager Instance;

    #region Properties
    /// <summary> Current total loot collected by the player. </summary>
    public int CurrentLootCount { get; private set; }
    #endregion

    #region Unity Events
    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Increases the loot count by 1.
    /// </summary>
    public void AddLoot()
    {
        CurrentLootCount++;
    }
    #endregion
}