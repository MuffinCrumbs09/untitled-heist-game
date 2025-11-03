using UnityEngine;

public class CustomTask : Task
{
    public void CompleteTask()
    {
        if (!isCompleted)
            isCompleted = true;
    }
    public override void UpdateTask()
    {
        
    }
}
