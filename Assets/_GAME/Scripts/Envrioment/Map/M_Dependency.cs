using System;
using UnityEngine;

[Serializable]
public class M_Dependency
{
    [Header("When This Area Has This Room Type")]
    public string TriggerRoomType;

    [Header("Then Another Area Must Have")]
    public string TargetAreaName;
    public string RequiredRoomType;
}