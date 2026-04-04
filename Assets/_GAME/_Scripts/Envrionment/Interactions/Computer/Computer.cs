using UnityEngine;
using Unity.Netcode;
using System.Collections;

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

    // Task
    public MinigameTask associatedTask;

    public NetworkVariable<bool> IsHacking = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsHacked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> TimeToHack = new(0);
    private int timeToHack => TimeToHack.Value;

    public bool CanInteract()
    {
        if (associatedTask == null) return enabled && !IsHacking.Value;

        foreach (var task in ObjectiveSystem.Instance.GetCurObjective().tasks)
        {
            if (task is MinigameTask minitask)
                if (minitask == associatedTask)
                    return true && !IsHacking.Value;
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
        // Server receives the RPC and broadcasts to all clients
        SubtitleManager.Instance.ShowNPCSubtitle("Contractor", CompleteText);
        IsHacked.Value = true;
        OnHackCompleteClientRpc();
    }

    [Rpc(SendTo.Server)]
    public void ResetComputerRpc(int time)
    {
        IsHacked.Value = false;
        IsHacking.Value = false;
        if(time != -1)
            TimeToHack.Value = time;
    }

    [ClientRpc]
    private void OnHackCompleteClientRpc()
    {
        associatedTask?.CompleteTask(ObjectiveSystem.Instance, ObjectiveSystem.Instance.CurrentObjectiveIndex.Value, ObjectiveSystem.Instance.GetCurObjective().tasks.IndexOf(associatedTask));
    }
}