using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  ObjectiveHintData.cs
//
//  ScriptableObject that pairs an (Objective, Task) index with an item-type
//  tag used for spawning hint-related items in the world.
//
//  Create via: Assets > Create > Map > Objective > Objective Hint Data
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Data asset that binds a mission phase (Objective + Task) to the type of
/// item that should be randomly spawned as a hint prop during that phase.
/// </summary>
[CreateAssetMenu(fileName = "New Objective Hint", menuName = "Map/Objective/Objective Hint Data")]
public class ObjectiveHintData : ScriptableObject
{
    #region Data Fields

    [Header("Mission Phase")]
    [Tooltip("X = Objective index, Y = Task index. Identifies which phase triggers this hint spawn.")]
    public Vector2Int Index;

    [Header("Item Spawning")]
    [Tooltip("The category tag used to find eligible spawn points and item prefabs in the scene.")]
    public ItemTypeTag SpawnItemType;

    #endregion
}