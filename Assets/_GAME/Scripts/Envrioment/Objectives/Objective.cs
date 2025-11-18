using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class Objective
{
    [Header("Objective Settings")]
    public string objectiveName;
    [SerializeReference] public List<Task> tasks;
    [Header("Subtitle")]
    public string speakerName;
    [TextArea] public string speech;

    private bool _hasStarted = false;

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
        if (!_hasStarted && NetworkManager.Singleton.IsServer) StartTask();

        foreach (var task in tasks)
        {
            task.UpdateTask();
        }
    }

    public void StartTask()
    {
        _hasStarted = true;
        if (speakerName == null || speech == null) return;
        SubtitleManager.Instance.ShowNPCSubtitle(speakerName, speech, 6.5f);
    }
}
