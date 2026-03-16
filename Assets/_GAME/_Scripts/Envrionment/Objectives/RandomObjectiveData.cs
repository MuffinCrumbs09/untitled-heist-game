using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Random Objective", menuName = "Map/Objective/Random Objective Data")]
public class RandomObjectiveData : ScriptableObject
{
    [Header("Trigger Settings")]
    // [Tooltip("The objective index at which this random objective becomes active.")]
    // public int ObjectiveId;

    [Tooltip("The type of room this objective takes place in.")]
    public RoomTypeTag RequiredRoomType;

    [Header("Item Spawning")]
    [Tooltip("The type of items to search for and randomly spawn.")]
    public ItemTypeTag SpawnItemType;

    [Tooltip("How many items to spawn. X = Min, Y = Max.")]
    public Vector2Int SpawnCountRange = new Vector2Int(1, 3);

    public int GetRandomSpawnCount()
    {
        return Random.Range(SpawnCountRange.x, SpawnCountRange.y + 1);
    }
}