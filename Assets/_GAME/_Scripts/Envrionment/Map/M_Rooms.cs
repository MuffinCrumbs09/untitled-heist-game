using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoomTypeLimit
{
    public RoomTypeTag RoomType;
    [Tooltip("X = Min, Y = Max (0 = unlimited)")]
    public Vector2 MinMax;
}

[CreateAssetMenu(fileName = "New Map Rooms", menuName = "Map/Area/Map Rooms")]
public class M_Rooms : ScriptableObject
{
    public List<RoomTypeLimit> Limits;
}
