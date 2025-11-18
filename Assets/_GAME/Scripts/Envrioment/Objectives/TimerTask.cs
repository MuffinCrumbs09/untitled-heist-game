using UnityEngine;

public class TimerTask : Task
{
    [Header("Timer Settings")]
    public float timerDuration;
    private float timer;

    public override void UpdateTask()
    {
        if (isCompleted) return;

        timer += Time.deltaTime;
        if (timer >= timerDuration)
            isCompleted = true;
    }
}
