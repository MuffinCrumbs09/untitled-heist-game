using UnityEngine;

[CreateAssetMenu(fileName = "New Map Areas", menuName = "Map Areas")]
public class M_Areas : ScriptableObject
{
    [Header("Area")]
    public string Area;

    [Header("Rooms")]
    public string[] Rooms;
    public bool CanBeHall;

    [Header("Dependencies")]
    public M_Dependency[] Dependencies;
}
