using UnityEngine;

public class MinigameTask : Task
{
    public void CompleteTask()
    {
        if (!isCompletled)
        {
            isCompletled = true;
        }
    }
    public override void UpdateTask()
    {

    }
}
