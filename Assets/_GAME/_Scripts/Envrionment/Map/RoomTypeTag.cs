using UnityEngine;

/// <summary>
/// A ScriptableObject used as a tag/label for room types.
/// This is intentionally empty — its identity as an asset IS the tag.
/// Create new tags via: Assets > Create > Map > Room Type Tag
/// </summary>
[CreateAssetMenu(fileName = "New Room Type Tag", menuName = "Map/Room Type Tag")]
public class RoomTypeTag : ScriptableObject { }