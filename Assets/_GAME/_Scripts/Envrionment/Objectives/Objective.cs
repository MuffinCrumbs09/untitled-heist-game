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

    /// <summary>
    /// Called every frame by ObjectiveSystem (server only).
    /// objectiveSystem reference is passed so tasks can call CompleteTask().
    /// </summary>
    public void UpdateObjective(ObjectiveSystem objectiveSystem)
    {
        // Server-only: fire the intro subtitle once.
        if (!_hasStarted && NetworkManager.Singleton.IsServer)
            StartTask();

        int objectiveIndex = objectiveSystem.ObjectiveList.IndexOf(this);

        for (int i = 0; i < tasks.Count; i++)
        {
            if (!tasks[i].isCompleted)
                tasks[i].UpdateTask(objectiveSystem, objectiveIndex, i);
        }
    }

    private void StartTask()
    {
        _hasStarted = true;
        if (string.IsNullOrEmpty(speakerName) || string.IsNullOrEmpty(speech)) return;
        SubtitleManager.Instance.ShowNPCSubtitle(speakerName, speech, 6.5f);
    }
}