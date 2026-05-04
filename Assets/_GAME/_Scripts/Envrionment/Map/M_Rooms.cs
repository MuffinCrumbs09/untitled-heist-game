using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a min/max spawn count constraint for a specific room type.
/// Used by M_Rooms to control how many of each room type can appear on a map.
/// </summary>
[Serializable]
public class RoomTypeLimit
{
    // The room type this limit applies to.
    public RoomTypeTag RoomType;

    // The minimum (X) and maximum (Y) number of times this room type can appear.
    // Set Y to 0 to indicate no upper limit (unlimited).
    [Tooltip("X = Min, Y = Max (0 = unlimited)")]
    public Vector2 MinMax;
}

/// <summary>
/// ScriptableObject that defines the spawn count limits for all room types on a map.
/// The map generator reads these limits to ensure the layout stays within
/// the defined minimum and maximum counts for each room type.
/// Create new room limit sets via: Assets > Create > Map > Area > Map Rooms
/// </summary>
[CreateAssetMenu(fileName = "New Map Rooms", menuName = "Map/Area/Map Rooms")]
public class M_Rooms : ScriptableObject
{
    // All room type limits for this map configuration.
    // Add one entry per room type you want to constrain.
    public List<RoomTypeLimit> Limits;
}