using UnityEngine;

/// <summary>
/// MonoBehaviour component attached to any item GameObject in the scene.
/// Links the item to its corresponding ItemTypeTag.
/// </summary>
public class ItemType : MonoBehaviour
{
    // The tag that identifies what kind of item this is.
    // Must be assigned in the Inspector — create tags via Map/Item/Item Type Tag.
    public ItemTypeTag Tag;
}