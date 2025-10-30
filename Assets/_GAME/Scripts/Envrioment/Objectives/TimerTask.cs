using UnityEngine;

public class TimerTask : Task
{
    [Header("Timer Settings")]
    public float timerDuration;
    private float timer;

    public override void UpdateTask()
    {
        if (isCompletled) return;

        timer += Time.deltaTime;
        if (timer >= timerDuration)
            isCompletled = true;
    }
}
