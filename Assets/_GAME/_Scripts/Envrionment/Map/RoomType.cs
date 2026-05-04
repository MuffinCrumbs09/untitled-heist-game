using UnityEngine;

/// <summary>
/// MonoBehaviour component attached to any room GameObject in the scene.
/// Links the room to its corresponding RoomTypeTag, which is used by the
/// map system to identify room types when evaluating area rules and dependencies.
/// </summary>
public class RoomType : MonoBehaviour
{
    // The tag that identifies what kind of room this is.
    // Must be assigned in the Inspector — create tags via Map/Room Type Tag.
    public RoomTypeTag Tag;
}