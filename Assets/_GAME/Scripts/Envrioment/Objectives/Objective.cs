using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Objective
{
    [Header("Objective Settings")]
    public string objectiveName;
    [SerializeReference] public List<Task> tasks;

    public bool IsCompleted()
    {
        foreach (var task in tasks)
        {
            if (!task.isCompleted)
                return false;
        }

        return true;
    }

    public void UpdateObjective()
    {
        foreach (var task in tasks)
        {
            task.UpdateTask();
        }
    }
}
