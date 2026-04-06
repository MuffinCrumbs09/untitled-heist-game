using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CircuitBreaker : NetworkBehaviour, IInteractable, IReady
{
    [Header("Configuration")]
    [SerializeField] private GameObject computerVisual;
    [SerializeField] private TMPro.TMP_Text serialNumberText;

    private const float HackDuration = 30f;

    public NetworkVariable<NetString> assignedSerial = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> correctBreaker = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool isCorrectBreaker => correctBreaker.Value;

    private bool isReady = false;

    private NetworkVariable<bool> isBeingHacked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isHackFinished = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #region Unity LifeCycle

    public override void OnNetworkSpawn()
    {
        isBeingHacked.OnValueChanged += OnHackingStateChanged;
        assignedSerial.OnValueChanged += OnSerialNumberChanged;

        if (!string.IsNullOrEmpty(assignedSerial.Value))
            serialNumberText.text = assignedSerial.Value;

        isReady = true;
    }

    public override void OnNetworkDespawn()
    {
        isBeingHacked.OnValueChanged -= OnHackingStateChanged;
        assignedSerial.OnValueChanged -= OnSerialNumberChanged;
    }

    #endregion

    /// <summary>
    /// Called by CircuitBreakerManager on server to assign identity data.
    /// </summary>
    public void Initialize(string serial, bool isCorrect)
    {
        if (!IsServer) return;

        assignedSerial.Value = serial;
        correctBreaker.Value = isCorrect;

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, string.Format("{0} : Serial: {1} | IsCorrect {2}", "[CircuitBreaker]".Color(Color.deepPink), serial, isCorrect), this);
#endif
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        StartHackRpc();
    }

    public string InteractText()
    {
        return CanInteract() ? "Hack Circuit Breaker" : string.Empty;
    }

    /// <summary>
    /// Interaction is available when the breaker is not already hacking or finished.
    /// </summary>
    public bool CanInteract()
    {
        return !isBeingHacked.Value && !isHackFinished.Value && !CircuitBreakerManager.Instance.IsHacking && CircuitBreakerManager.Instance.IsObjective();
    }

    // ── Network ──────────────────────────────────────────────────────────────

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void StartHackRpc()
    {
        if (isBeingHacked.Value || isHackFinished.Value) return;

        isBeingHacked.Value = true;
        isHackFinished.Value = true; // Lock immediately so no second interaction can slip through
        StartCoroutine(HackCoroutine());
    }

    private IEnumerator HackCoroutine()
    {
        yield return new WaitForSeconds(HackDuration);
        OnHackCompleteRpc(isCorrectBreaker);
    }

    [Rpc(SendTo.Everyone)]
    private void OnHackCompleteRpc(bool wasCorrect)
    {
        computerVisual.SetActive(false);

        if (wasCorrect && ObjectiveSystem.Instance != null)
        {
            Objective cur = ObjectiveSystem.Instance.GetCurObjective();

            foreach (var task in cur.tasks)
            {
                if (task is CustomTask custom)
                    if (!custom.isCompleted)
                    {
                        custom.CompleteTask(ObjectiveSystem.Instance, ObjectiveSystem.Instance.CurrentObjectiveIndex.Value, cur.tasks.IndexOf(task));
                        break;
                    }
            }
        }
    }

    // ── Visual sync ──────────────────────────────────────────────────────────

    private void OnHackingStateChanged(bool previous, bool current)
    {
        computerVisual.SetActive(current);
    }

    private void OnSerialNumberChanged(NetString previous, NetString current)
    {
        serialNumberText.text = current;
    }

    public bool IsReady()
    {
        return isReady;
    }
}
