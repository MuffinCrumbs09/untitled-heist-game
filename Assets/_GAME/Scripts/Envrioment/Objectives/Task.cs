using UnityEngine;

[System.Serializable]
public abstract class Task
{
    [Header("Settings")]
    public string taskName;
    public bool isCompleted;

    public abstract void UpdateTask();
}
