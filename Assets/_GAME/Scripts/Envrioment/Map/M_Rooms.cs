using UnityEngine;

[CreateAssetMenu(fileName = "New Map Rooms", menuName = "Map Rooms")]
public class M_Rooms : ScriptableObject
{
    [Header("Rooms - X = min, Y = max")]
    public Vector2 Vault;
    public Vector2 Security;
    public Vector2 Office;
    public Vector2 Hall;
}
