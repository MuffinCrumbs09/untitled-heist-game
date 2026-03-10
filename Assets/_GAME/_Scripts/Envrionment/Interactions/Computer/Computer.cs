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
    [SerializeField] private int TimeToHack;
    public NetworkVariable<bool> IsHacked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool CanInteract()
    {
        if (associatedTask == null) return enabled && !IsHacked.Value;

        foreach (var task in ObjectiveSystem.Instance.GetCurObjective().tasks)
        {
            if (task is MinigameTask minitask)
                if (minitask == associatedTask)
                    return true && !IsHacked.Value;
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
        IsHacked.Value = true;
        StartCoroutine(StartHack());
    }

    private IEnumerator StartHack()
    {
        SubtitleManager.Instance.ShowNPCSubtitle("Contractor", $"Hack Starting. {TimeToHack} seconds remaining.");
        yield return new WaitForSeconds(TimeToHack);
        OnHackCompleteServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void OnHackCompleteServerRpc()
    {
        // Server receives the RPC and broadcasts to all clients
        SubtitleManager.Instance.ShowNPCSubtitle("Contractor", CompleteText);
        OnHackCompleteClientRpc();
    }

    [ClientRpc]
    private void OnHackCompleteClientRpc()
    {
        if (associatedTask != null)
        {
            associatedTask.CompleteTask();
        }

        Destroy(this);
    }
}