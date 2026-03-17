using UnityEngine;

public class MinigameTask : Task
{
    public string RoomType;

    public bool setComputer = true;
    
    [Header("Settings - Random")]
    public bool isRandomComputer = false;
    public Vector2 MinMax;
    public void CompleteTask()
    {
        if (!isCompleted)
        {
            isCompleted = true;
        }
    }
    public override void UpdateTask() { }
}
