using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A network-synced object that players can interact with to complete a objective.
/// </summary>
public class CircuitBreaker : NetworkBehaviour, IInteractable, IReady
{
    #region Variables

    [Header("Configuration")]
    [SerializeField] 
    [Tooltip("Visual representation of the computer/terminal used during the hack.")]
    private GameObject computerVisual;

    [SerializeField] 
    [Tooltip("Text component that displays the unique serial number of this breaker.")]
    private TMPro.TMP_Text serialNumberText;

    private const float HackDuration = 30f;

    /// <summary> The network-synced serial number assigned to this breaker. </summary>
    public NetworkVariable<NetString> assignedSerial = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    private NetworkVariable<bool> correctBreaker = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool isCorrectBreaker => correctBreaker.Value;
    private bool isReady = false;

    private NetworkVariable<bool> isBeingHacked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isHackFinished = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #endregion

    #region Network Lifecycle

    /// <summary>
    /// Subscribes to network variable changes and initializes state when spawned on the network.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        isBeingHacked.OnValueChanged += OnHackingStateChanged;
        assignedSerial.OnValueChanged += OnSerialNumberChanged;

        if (!string.IsNullOrEmpty(assignedSerial.Value))
            serialNumberText.text = assignedSerial.Value;

        isReady = true;
    }

    /// <summary>
    /// Cleans up network event subscriptions.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        isBeingHacked.OnValueChanged -= OnHackingStateChanged;
        assignedSerial.OnValueChanged -= OnSerialNumberChanged;
    }

    #endregion

    #region Interaction Logic

    /// <summary>
    /// Server-side initialization of breaker identity.
    /// </summary>
    public void Initialize(string serial, bool isCorrect)
    {
        if (!IsServer) return;
        assignedSerial.Value = serial;
        correctBreaker.Value = isCorrect;
    }

    /// <summary> Initiates the interaction. </summary>
    public void Interact()
    {
        if (CanInteract()) StartHackRpc();
    }

    /// <summary> Returns UI text for interaction prompts. </summary>
    public string InteractText() => CanInteract() ? "Hack Circuit Breaker" : string.Empty;

    /// <summary>
    /// Checks if the breaker is valid for interaction based on hacking state and active objectives.
    /// </summary>
    public bool CanInteract()
    {
        return !isBeingHacked.Value && !isHackFinished.Value && !CircuitBreakerManager.Instance.IsHacking && CircuitBreakerManager.Instance.IsObjective();
    }

    #endregion

    #region RPCs & Coroutines

    /// <summary>
    /// Server-side RPC to start the hack process and broadcast state changes.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void StartHackRpc()
    {
        if (isBeingHacked.Value || isHackFinished.Value) return;

        isBeingHacked.Value = true;
        isHackFinished.Value = true;
        CircuitBreakerManager.Instance.SetHackingStateRpc(true);
        SubtitleManager.Instance.ShowNPCSubtitle("Contractor", "Hack Started. 30 seconds");
        SetComputerVisualRpc(true);
        StartCoroutine(HackCoroutine());
    }

    /// <summary>
    /// Waits for the hack duration and then reports completion to the server.
    /// </summary>
    private IEnumerator HackCoroutine()
    {
        yield return new WaitForSeconds(HackDuration);
        CircuitBreakerManager.Instance.SetHackingStateRpc(false);
        OnHackCompleteRpc(isCorrectBreaker);
    }

    /// <summary> Syncs the visibility of the computer model across all clients. </summary>
    [Rpc(SendTo.Everyone)]
    private void SetComputerVisualRpc(bool active) => computerVisual.SetActive(active);

    /// <summary>
    /// Handles the result of the hack, updating objectives if correct or showing a failure message if wrong.
    /// </summary>
    [Rpc(SendTo.Everyone)]
    private void OnHackCompleteRpc(bool wasCorrect)
    {
        SetComputerVisualRpc(false);

        if(!NetworkManager.Singleton.IsServer) return;

        if (wasCorrect && ObjectiveSystem.Instance != null)
        {
            Objective cur = ObjectiveSystem.Instance.GetCurObjective();
            foreach (var task in cur.tasks)
            {
                if (task is CustomTask custom && !custom.isCompleted)
                {
                    custom.CompleteTask(ObjectiveSystem.Instance, ObjectiveSystem.Instance.CurrentObjectiveIndex.Value, cur.tasks.IndexOf(task));
                    break;
                }
            }
        }
        else if(!wasCorrect)
        {
            SubtitleManager.Instance.ShowNPCSubtitle("Contractor", "Wrong breaker. Try another.");
        }
    }

    #endregion

    #region Callbacks

    private void OnHackingStateChanged(bool previous, bool current) => computerVisual.SetActive(current);
    private void OnSerialNumberChanged(NetString previous, NetString current) => serialNumberText.text = current;
    public bool IsReady() => isReady;

    #endregion
}