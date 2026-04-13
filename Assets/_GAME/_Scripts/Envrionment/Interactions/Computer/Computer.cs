using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.VisualScripting;

public enum ComputerType
{
    TIMER,
    CODE
}

public class Computer : NetworkBehaviour, IInteractable
{
    [Header("Settings")]
    public HackingMinigame minigame;
    public ComputerType type;
    [TextArea(3, 10)] public string CompleteText;

    public MinigameTask associatedTask;

    public NetworkVariable<bool> IsHacking = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsHacked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> Interactable = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> TimeToHack = new(0);
    private int timeToHack => TimeToHack.Value;

    private ComputerSettings _settings;

    private void Awake()
    {
        _settings = GetComponent<ComputerSettings>();
    }

    public bool CanInteract()
    {
        if (IsHacked.Value || IsHacking.Value) return false;

        // Correctly assigned computer on the right objective — always interactable
        if (associatedTask != null)
        {
            Objective curObjective = ObjectiveSystem.Instance.GetCurObjective();
            int currentTaskIndex = curObjective.GetCurrentTaskIndex();

            if (currentTaskIndex >= 0)
            {
                Task currentTask = curObjective.tasks[currentTaskIndex];

                if (currentTask is MinigameTask minigameTask && minigameTask == associatedTask)
                    return true;
            }
        }  

        // Everything else goes through the network variable
        if (!Interactable.Value) return false;

        return true;
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

    public bool IsCorrectComputer() => associatedTask != null;

    public void OnHackComplete()
    {
        if (!IsCorrectComputer())
        {
            int index = Random.Range(0, MapManager.Instance.MapRandomDialouge.ComputerDialouge.Count);
            SubtitleManager.Instance.ShowNPCSubtitle("Contractor", MapManager.Instance.MapRandomDialouge.ComputerDialouge[index], 5);
            OnHackCompleteServerRpc();
        }
        else
        {
            if (type == ComputerType.CODE)
                OnHackCompleteServerRpc();
            else if (type == ComputerType.TIMER)
                StartHackRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void StartHackRpc()
    {
        IsHacking.Value = true;
        StartCoroutine(StartHack());
    }

    private IEnumerator StartHack()
    {
        SubtitleManager.Instance.ShowNPCSubtitle("Contractor", $"Hack Starting. {timeToHack} seconds remaining.");
        yield return new WaitForSeconds(timeToHack);
        OnHackCompleteServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void OnHackCompleteServerRpc()
    {
        IsHacked.Value = true;
        Interactable.Value = false;
        OnHackCompleteClientRpc();
    }

    [Rpc(SendTo.Server)]
    public void ResetComputerRpc(int time)
    {
        IsHacked.Value = false;
        IsHacking.Value = false;
        Interactable.Value = true;
        if (time != -1)
            TimeToHack.Value = time;
    }

    [Rpc(SendTo.NotServer)]
    public void SyncAssociatedTaskClientRpc(int objectiveIndex, int taskIndex)
    {
        if (ObjectiveSystem.Instance == null) return;

        var objective = ObjectiveSystem.Instance.ObjectiveList[objectiveIndex];
        if (objective == null || taskIndex < 0 || taskIndex >= objective.tasks.Count) return;

        if (objective.tasks[taskIndex] is MinigameTask miniTask)
            associatedTask = miniTask;
    }

    [ClientRpc]
    private void OnHackCompleteClientRpc()
    {
        if (associatedTask == null) return;

        associatedTask.CompleteTask(
            ObjectiveSystem.Instance,
            ObjectiveSystem.Instance.CurrentObjectiveIndex.Value,
            ObjectiveSystem.Instance.GetCurObjective().tasks.IndexOf(associatedTask)
        );

        SubtitleManager.Instance.ShowNPCSubtitle("Contractor", CompleteText);
    }
}