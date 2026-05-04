using UnityEngine;

/// <summary>
/// Provides a list of player spawn point Transforms sourced from this GameObject's children.
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    /// <summary>
    /// Returns all direct child Transforms as an array of spawn points.
    /// Each child's position and rotation will be used when placing a player into the world.
    /// Add or remove spawn points by adding or removing child GameObjects in the hierarchy.
    /// </summary>
    public Transform[] Points
    {
        get
        {
            Transform[] pts = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                pts[i] = transform.GetChild(i);
            return pts;
        }
    }
}