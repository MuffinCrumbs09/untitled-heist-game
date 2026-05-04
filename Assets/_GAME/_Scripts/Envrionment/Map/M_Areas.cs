using UnityEngine;

/// <summary>
/// ScriptableObject that defines a single named area on the map.
/// An area represents a distinct zone and specifies which room types it can contain,
///  whether it can act as a hallway connector, and any dependency rules.
/// Create new areas via: Assets > Create > Map > Area > Map Areas
/// </summary>
[CreateAssetMenu(fileName = "New Map Areas", menuName = "Map/Area/Map Areas")]
public class M_Areas : ScriptableObject
{
    // The unique name identifier for this area.
    // Must match any Area string references used in M_AreaItem or M_Dependency assets.
    public string Area;

    // The set of room types this area is allowed to contain.
    // The map generator picks from these tags when populating the area with rooms.
    public RoomTypeTag[] Rooms;

    // If true, this area can be used as a hallway/connector between other areas.
    public bool CanBeHall;

    // Optional list of dependency rules for this area.
    // Each dependency defines a condition: if this area has a certain room type,
    // then another named area must also have a specific room type.
    public M_Dependency[] Dependencies;
}