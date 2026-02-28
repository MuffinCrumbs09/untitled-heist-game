using System;
using UnityEngine;

[Serializable]
public class M_Dependency
{
    [Header("When This Area Has This Room Type")]
    public RoomTypeTag TriggerRoomType;

    [Header("Then Another Area Must Have")]
    public string TargetAreaName;
    public RoomTypeTag RequiredRoomType;
}