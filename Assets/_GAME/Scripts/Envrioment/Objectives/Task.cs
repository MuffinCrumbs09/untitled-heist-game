using UnityEngine;

public abstract class Task
{
    [Header("Settings")]
    public string taskName;
    public bool isCompletled;

    public abstract void UpdateTask();
}
