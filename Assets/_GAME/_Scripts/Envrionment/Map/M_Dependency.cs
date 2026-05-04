using System;

/// <summary>
/// Serializable data class representing a cross-area dependency rule.
/// Works as an "if/then" constraint during map generation
/// Used within M_Areas to enforce logical relationships between areas
/// </summary>
[Serializable]
public class M_Dependency
{
    // The room type in the owning area that activates this dependency.
    // If the owning area contains a room with this tag, the rule below is enforced.
    public RoomTypeTag TriggerRoomType;

    // The name of the other area that must satisfy the requirement.
    // Must match the Area string of an existing M_Areas asset.
    public string TargetAreaName;

    // The room type that the target area must contain when this rule is active.
    public RoomTypeTag RequiredRoomType;
}