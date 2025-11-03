using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LocationMapping
{
    public string RoomObjectName;
    public string LocationObjectName;
}

[CreateAssetMenu(fileName = "New Map Locations", menuName = "Map Locations")]
public class M_Locations : ScriptableObject
{
    [Header("What Room?")]
    public string RoomName;

    [Header("Locations")]
    public List<LocationMapping> LocationNames = new();
}
