using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  RandomObjectiveData.cs
//
//  ScriptableObject that configures a randomly-placed objective item spawn.
//  Used by whatever system handles random objective placement to know which
//  room type to target, what items to spawn, and how many to place.
//
//  Create via: Assets > Create > Map > Objective > Random Objective Data
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Data asset that describes the parameters for a random item-spawn objective.
/// Defines the required room type, item category, and quantity range.
/// </summary>
[CreateAssetMenu(fileName = "New Random Objective", menuName = "Map/Objective/Random Objective Data")]
public class RandomObjectiveData : ScriptableObject
{
    #region Data Fields

    [Header("Room Requirements")]
    [Tooltip("Only rooms with this tag are eligible for this objective's item placement.")]
    public RoomTypeTag RequiredRoomType;

    [Header("Item Spawning")]
    [Tooltip("Category tag used to locate matching item prefabs and spawn points in the scene.")]
    public ItemTypeTag SpawnItemType;

    [Tooltip("The number of items to spawn is chosen uniformly at random between X (min) and Y (max), inclusive.")]
    public Vector2Int SpawnCountRange = new Vector2Int(1, 3);

    #endregion

    #region Public API

    /// <summary>
    /// Returns a random spawn count within the configured min/max range (inclusive).
    /// </summary>
    public int GetRandomSpawnCount()
    {
        return Random.Range(SpawnCountRange.x, SpawnCountRange.y + 1);
    }

    #endregion
}