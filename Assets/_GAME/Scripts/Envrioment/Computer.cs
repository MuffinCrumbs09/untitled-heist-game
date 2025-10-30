using UnityEngine;

public class Computer : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public MinigameTask associatedTask;
    public HackingMinigame minigame;

    public void Interact()
    {
        minigame.StartHacking(this);
    }

    public string InteractText()
    {
        return "Press [E] to Hack Computer";
    }

    public bool IsCorrectComputer()
    {
        return associatedTask != null;
    }

    public void OnHackComplete()
    {
        if (associatedTask != null)
        {
            associatedTask.CompleteTask();
            Debug.Log($"Completed task: {associatedTask.taskName}");
        }
        else
        {
            Debug.Log("Hacked decoy computer - no task completed");
        }

        Destroy(this);
    }
}
