using UnityEngine;

/// <summary>
/// ScriptableObject that defines a conditional item spawn rule for a specific area.
/// Works as an "if/then" rule: if a given area contains a specific room type,
/// then a particular item type should be spawned in another specified room type.
/// Create new rules via: Assets > Create > Map > Item > Area Item
/// </summary>
[CreateAssetMenu(fileName = "New Area Item", menuName = "Map/Item/Area Item")]
public class M_AreaItem : ScriptableObject
{
    // The name of the area this rule applies to.
    // Must match the Area string defined in the corresponding M_Areas asset.
    public string Area;

    // The room type that must be present in the area to trigger this rule.
    // If the area contains a room with this tag, the item spawn below will be scheduled.
    public RoomTypeTag TriggerRoomType;

    // The type of item that should be spawned when the rule is triggered.
    public ItemTypeTag ItemType;

    // The type of room the item should be spawned inside.
    // The map generator will look for a room matching this tag to place the item.
    public RoomTypeTag SpawnRoomType;
}