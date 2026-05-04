using UnityEngine;
using Unity.Netcode;
using System.Collections;

public enum ComputerType { TIMER, CODE }

/// <summary>
/// Manages the hacking logic for computers, linking them to mission objectives 
/// and handling networked hacking states.
/// </summary>
public class Computer : NetworkBehaviour, IInteractable
{
    [Header("Settings")]
    [Tooltip("The hacking minigame UI script.")] public HackingMinigame minigame;
    [Tooltip("Determines if the hack is instant (CODE) or takes time (TIMER).")] public ComputerType type;
    [TextArea(3, 10)] public string CompleteText;

    [Header("Mission Logic")]
    [Tooltip("The specific task this computer completes.")] public MinigameTask associatedTask;

    [Header("Network States")]
    public NetworkVariable<bool> IsHacking = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsHacked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> Interactable = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> TimeToHack = new(0);

    private ComputerSettings _settings;

    private void Awake() => _settings = GetComponent<ComputerSettings>();

    #region Interface

    /// <summary>
    /// Checks if the computer is currently available to be hacked based on objective progress.
    /// </summary>
    public bool CanInteract()
    {
        if (IsHacked.Value || IsHacking.Value) return false;

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

        return Interactable.Value;
    }

    /// <summary>
    /// Opens the hacking minigame interface.
    /// </summary>
    public void Interact()
    {
        if (!CanInteract()) return;
        minigame.StartHacking(this);
    }

    /// <summary>
    /// Returns the prompt text for the player's interaction HUD.
    /// </summary>
    public string InteractText() => CanInteract() ? "Press [E] to Hack Computer" : string.Empty;

    #endregion

    /// <summary>
    /// Logic triggered when the player successfully completes the hacking minigame.
    /// </summary>
    public void OnHackComplete()
    {
        if (associatedTask == null)
        {
            // Play flavor dialogue if this isn't a mission-critical computer
            int index = Random.Range(0, MapManager.Instance.MapRandomDialouge.ComputerDialouge.Count);
            SubtitleManager.Instance.ShowNPCSubtitle("Contractor", MapManager.Instance.MapRandomDialouge.ComputerDialouge[index], 5);
            OnHackCompleteServerRpc();
        }
        else
        {
            if (type == ComputerType.CODE) OnHackCompleteServerRpc();
            else if (type == ComputerType.TIMER) StartHackRpc();
        }
    }

    #region RPCs & Coroutines

    /// <summary>
    /// Starts a timed background hack on the server.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void StartHackRpc()
    {
        IsHacking.Value = true;
        StartCoroutine(StartHackRoutine());
    }

    private IEnumerator StartHackRoutine()
    {
        if (TimeToHack.Value != 0)
            SubtitleManager.Instance.ShowNPCSubtitle("Contractor", $"Hack Starting. {TimeToHack.Value} seconds remaining.");
        
        yield return new WaitForSeconds(TimeToHack.Value);
        OnHackCompleteServerRpc();
    }

    /// <summary>
    /// Finalizes the hack on the server and notifies clients to update objective progress.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void OnHackCompleteServerRpc()
    {
        IsHacked.Value = true;
        Interactable.Value = false;
        
        if (CompleteText != string.Empty && associatedTask != null)
            SubtitleManager.Instance.ShowNPCSubtitle("Contractor", CompleteText);
            
        OnHackCompleteClientRpc();
    }

    /// <summary>
    /// Updates the objective system on all clients once the server confirms completion.
    /// </summary>
    [ClientRpc]
    private void OnHackCompleteClientRpc()
    {
        if (associatedTask == null) return;

        associatedTask.CompleteTask(
            ObjectiveSystem.Instance,
            ObjectiveSystem.Instance.CurrentObjectiveIndex.Value,
            ObjectiveSystem.Instance.GetCurObjective().tasks.IndexOf(associatedTask)
        );
    }

    #endregion
}