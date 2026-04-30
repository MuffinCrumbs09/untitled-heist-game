using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class OutOfBounds : MonoBehaviour
{
    public Transform spawnPoint;
    /// <summary>
    /// OnTriggerEnter is called when the Collider other enters the trigger.
    /// </summary>
    /// <param name="other">The other Collider involved in this collision.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out NetworkObject networkObject))
            {
                if (!networkObject.IsOwner)
                    return;

                networkObject.GetComponent<PlayerMovement>().enabled = false;
                networkObject.transform.position = spawnPoint.position;
                StartCoroutine(AllowMovement(networkObject));
            }
        }
    }

    private IEnumerator AllowMovement(NetworkObject obj)
    {
        yield return new WaitForSeconds(1f);
        obj.GetComponent<PlayerMovement>().enabled = true;
    }
}
