using System.Collections.Generic;
using UnityEngine;

public class Sensor : MonoBehaviour
{
    [Header("Settings")]
    public List<string> Tags = new();
    public float viewDeg;
    public float viewDist;
    public LayerMask obstacleMask;

    public GameObject InView()
    {
        foreach (string s in Tags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(s);
            foreach (GameObject obj in objects)
            {
                Vector3 dirToTarget = obj.transform.position - transform.position;
                float dist = dirToTarget.magnitude;

                if (dist > viewDist)
                    continue;

                float angle = Vector3.Angle(transform.forward, dirToTarget);
                if (angle > viewDeg * 0.5f)
                    continue;

                if (!Physics.Raycast(transform.position, dirToTarget.normalized, dist, obstacleMask))
                    return obj;
            }
        }

        return null;
    }

        private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDist);

        Vector3 left = Quaternion.Euler(0, -viewDeg * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewDeg * 0.5f, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * viewDist);
        Gizmos.DrawLine(transform.position, transform.position + right * viewDist);
    }
}
