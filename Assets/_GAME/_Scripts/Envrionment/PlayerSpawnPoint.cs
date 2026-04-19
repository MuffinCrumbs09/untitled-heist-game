using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
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