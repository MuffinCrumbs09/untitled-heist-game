using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CircuitBreaker : NetworkBehaviour, IInteractable, IReady
{
    [Header("Configuration")]
    [SerializeField] private GameObject computerVisual;
    [SerializeField] private TMPro.TMP_Text serialNumberText;

    private const float HackDuration = 30f;

    private string assignedSerial;
    private bool isCorrectBreaker;

    private bool isReady = false;

    private NetworkVariable<bool> isBeingHacked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isHackFinished = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #region Unity LifeCycle

    public override void OnNetworkSpawn()
    {
        isBeingHacked.OnValueChanged += OnHackingStateChanged;
        isReady = true;
    }

    public override void OnNetworkDespawn()
    {
        isBeingHacked.OnValueChanged -= OnHackingStateChanged;
    }

    #endregion

    public void InitObjects()
    {
        transform.parent.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called by CircuitBreakerManager on both server and clients to assign identity data.
    /// </summary>
    public void Initialize(string serial, bool isCorrect)
    {
        assignedSerial = serial;
        isCorrectBreaker = isCorrect;
        serialNumberText.text = assignedSerial;
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
        return !isBeingHacked.Value && !isHackFinished.Value && !CircuitBreakerManager.Instance.IsHacking && enabled;
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
                        custom.CompleteTask();
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

    public bool IsReady()
    {
        return isReady;
    }
}
