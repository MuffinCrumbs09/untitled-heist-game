using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Objective Hint", menuName = "Map/Objective/Objective Hint Data")]
public class ObjectiveHintData : ScriptableObject
{
    [Header("Objective Data")]
    [Tooltip("The Objective Index and Task Index")]
    public Vector2Int Index;

    [Header("Item Spawning")]
    [Tooltip("The type of items to search for and randomly spawn.")]
    public ItemTypeTag SpawnItemType;
}