using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps a room GameObject name to one or more named objective location objects within it.
/// </summary>
[System.Serializable]
public class LocationMapping
{
    // The name of the room GameObject in the scene (must match exactly).
    public string RoomObjectName;

    // The name of the child object inside to use as the objective location.
    public string LocationObjectName;
}

/// <summary>
/// ScriptableObject that defines all valid objective locations for a specific room.
/// Associates a room (by name) with a list of LocationMappings, each pairing
/// a room object to a named location point within it.
/// Create new location sets via: Assets > Create > Map > Area > Map Locations
/// </summary>
[CreateAssetMenu(fileName = "New Map Locations", menuName = "Map/Area/Map Locations")]
public class M_Locations : ScriptableObject
{
    // The name of the room this asset defines locations for.
    // Should match the room's GameObject name or identifier used by the map system.
    public string RoomName;

    // All valid objective locations within this room.
    public List<LocationMapping> LocationNames = new();
}