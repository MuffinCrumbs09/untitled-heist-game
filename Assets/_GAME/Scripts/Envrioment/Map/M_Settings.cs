using UnityEngine;

[CreateAssetMenu(fileName = "New Map Settings", menuName = "Map Settings")]
public class M_Settings : ScriptableObject
{
    [Header("Settings")]
    public M_Rooms MapRooms;
    [Header("Metadeta")]
    public string MapName;
    [TextArea] public string MapDesc;
    public string Contractor;
    public int MaxPayout;
}
