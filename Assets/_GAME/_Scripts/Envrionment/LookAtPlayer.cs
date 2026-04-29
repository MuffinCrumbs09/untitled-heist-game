using Unity.Netcode;
using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    Transform player;

    bool _hasSet = false;

    void Update()
    {
        if (!_hasSet)
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                player = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
                _hasSet = true;
            }
            else
                return;


        transform.LookAt(player);

    }
}
