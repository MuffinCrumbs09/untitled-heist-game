using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Area Item", menuName = "Map/Item/Area Item")]
public class M_AreaItem : ScriptableObject
{
    [Header("When This Area Has This Room Type")]
    public string Area;
    public RoomTypeTag TriggerRoomType;

    [Header("Then This Item Should Spawn In This Location")]
    public ItemTypeTag ItemType;
    public RoomTypeTag SpawnRoomType;
}