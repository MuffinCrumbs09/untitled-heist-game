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

            switch (type)
            {
                case ComputerType.TIMER:
                    {
                        timer.StartTimer();
                        break;
                    }
                case ComputerType.CODE:
                    {
                        // Subtitle
                        Debug.Log("Code is blah blah blah");
                        break;
                    }
            }
        }
        else
        {
            // subtitle
            Debug.Log("One of many possible things contractor will \"find\"");
        }

        Destroy(this);
    }
}
