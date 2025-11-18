using System.Collections.Generic;
using UnityEngine;

public class LocationTask : Task
{
    public List<Transform> possibleAreas = new();

    public void CompleteTask()
    {
        if (!isCompleted)
        {
            isCompleted = true;
        }
    }
    public override void UpdateTask()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            foreach(Transform area in possibleAreas)
            {
                if (Vector3.Distance(player.transform.position, area.position) <= 2.1f)
                    isCompleted = true;
            }
        }
    }
}
