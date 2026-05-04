using UnityEngine;

/// <summary>
/// ScriptableObject that acts as the top-level configuration asset for a single map..
/// Create new map settings via: Assets > Create > Map > Map Settings
/// </summary>
[CreateAssetMenu(fileName = "New Map Settings", menuName = "Map/Map Settings")]
public class M_Settings : ScriptableObject
{
    // Defines the room type limits for this map (min/max counts per room type).
    public M_Rooms MapRooms;

    // Each M_Locations asset describes valid location points for location tasks
    public M_Locations[] MapLocations;

    // The display name of this map, shown in menus or the UI.
    public string MapName;

    // A short description of the map shown to the player (e.g. in a contract briefing).
    [TextArea] public string MapDesc;

    // The name of the contractor that issued this map's mission.
    public string Contractor;

    // The maximum credit/currency payout the player can earn on this map.
    public int MaxPayout;
}