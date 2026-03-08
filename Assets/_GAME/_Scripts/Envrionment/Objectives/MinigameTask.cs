using UnityEngine;

public class MinigameTask : Task
{
    public string RoomType;
    public void CompleteTask()
    {
        if (!isCompleted)
        {
            isCompleted = true;
        }
    }
    public override void UpdateTask() { }
}
