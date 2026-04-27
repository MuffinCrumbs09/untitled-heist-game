using Unity.Netcode;
using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    Transform player;

    void Start()
    {
        player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Transform>();
    }

    void Update()
    {
        if (player != null)
        {
            transform.LookAt(player);
        }
    }
}
