using UnityEngine;

/// <summary>
/// A ScriptableObject used as a tag/label for item types.
/// This is intentionally empty — its identity as an asset IS the tag.
/// Create new tags via: Assets > Create > Map > Item > Item Type Tag
/// </summary>
[CreateAssetMenu(fileName = "New Item Type Tag", menuName = "Map/Item/Item Type Tag")]
public class ItemTypeTag : ScriptableObject { }