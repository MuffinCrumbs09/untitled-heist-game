using System;
using TMPro;
using UnityEngine;

public class DoorTimer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float TimeToWait;
    [SerializeField] private TMP_Text Text;

    private float time;
    private bool _hasStarted;

    public void StartTimer() => _hasStarted = true;

    private void Start()
    {
        Text.text = TimeToWait.ToString();
    }

    private void Update()
    {
        if (_hasStarted)
            TickTimer();
    }

    private void TickTimer()
    {
        if (time >= TimeToWait)
            OpenDoor();

        Text.text = string.Format("{0}", (int)TimeToWait - (int)time);
        time += Time.deltaTime;
    }

    private void OpenDoor()
    {
        Objective cur = ObjectiveSystem.Instance.GetCurObjective();

        foreach (var task in cur.tasks)
        {
            if (task is CustomTask custom)
                if (!custom.isCompleted)
                {
                    custom.CompleteTask();
                    break;
                }

        }

        enabled = false;
    }
}
