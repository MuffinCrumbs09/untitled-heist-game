using System.Collections.Generic;
using UnityEngine;

public class ObjectiveSystem : MonoBehaviour
{
    public List<Objective> ObjectiveList = new();

    private int curObj = 0;

    private void Update()
    {
        if (curObj == ObjectiveList.Count) return;
        
        ObjectiveList[curObj].UpdateObjective();

        if (ObjectiveList[curObj].IsCompleted())
        {
            Debug.Log("Completed Objective " + curObj);
            curObj++;
        }
    }
}
