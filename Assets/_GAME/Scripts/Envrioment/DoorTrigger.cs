using UnityEngine;
using UnityEngine.AI;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField] Door door;
    void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out NavMeshAgent agent))
        {
            Debug.Log("Here!");
            if (!door.isOpen.Value)
                door.ToggleDoorServerRpc();
        }
    }
}
