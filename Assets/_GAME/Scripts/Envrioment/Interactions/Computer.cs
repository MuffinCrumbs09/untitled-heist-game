using UnityEngine;

[SerializeField]
public enum ComputerType
{
    TIMER,
    CODE
}

public class Computer : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public HackingMinigame minigame;
    public ComputerType type;

    // Task
    public MinigameTask associatedTask;
    [HideInInspector] public DoorTimer timer;

    public bool CanInteract()
    {
        if (associatedTask == null) return enabled;
 
        foreach (var task in ObjectiveSystem.Instance.GetCurObjective().tasks)
        {
            if (task is MinigameTask minitask)
                if (minitask == associatedTask)
                    return true;
        }

        return false;
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        minigame.StartHacking(this);
    }

    public string InteractText()
    {
        if (!CanInteract()) return string.Empty;

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

            switch (type)
            {
                case ComputerType.TIMER:
                    {
                        timer.StartTimer();
                        break;
                    }
                case ComputerType.CODE:
                    {
                        SubtitleManager.Instance.ShowNPCSubtitle("Contractor", "Code is blah blah blah");
                        break;
                    }
            }
        }
        else
        {
            // subtitle
            int index = Random.Range(0, MapManager.Instance.MapRandomDialouge.ComputerDialouge.Count);
            SubtitleManager.Instance.ShowNPCSubtitle("Contractor", MapManager.Instance.MapRandomDialouge.ComputerDialouge[index], 5);
        }

        Destroy(this);
    }
}
