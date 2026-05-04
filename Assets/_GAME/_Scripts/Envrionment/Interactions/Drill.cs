using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Drill interaction that opens a door over time.
/// Uses network sync for drilling state and countdown.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class Drill : NetworkBehaviour, IInteractable
{
    [Header("Drill - Settings")]
    [Tooltip("The time required to drill through the door."), SerializeField] private float TimeToDrill;
    [Tooltip("The door object to be opened."), SerializeField] private GameObject Door;
    [Tooltip("The position to open the door to."), SerializeField] private Vector3 DoorOpen;
    [Tooltip("The text component for the drill timer."), SerializeField] private TMP_Text DrillText;

    [Header("Interaction")]
    [Tooltip("The number of times the player must click to place drill."), SerializeField] private int clickAmount = 1;
    [Tooltip("The UI component for displaying interaction progress."), SerializeField] private InteractionProgressUI progressUI;

    [Header("Network Variables")]
    public NetworkVariable<float> TimeRemaining = new(0);
    public NetworkVariable<bool> _IsDrilling = new(false);
    public NetworkVariable<bool> IsJammed = new(false);

    private bool IsOpen => TimeRemaining.Value <= 0;
    private bool IsDrilling => _IsDrilling.Value;
    private bool opened;

    private Quaternion _doorOpen;

    private int clickTimes = 0;
    private bool isPlayerNearby;

    #region Unity LifeCycle
    public override void OnNetworkSpawn()
    {
        if (IsServer)
            TimeRemaining.Value = TimeToDrill;
    }

    private void Start()
    {
        _doorOpen = Quaternion.Euler(DoorOpen);
        DrillText.text = TimeToDrill.ToString();

        progressUI.SetButtonText("E");
        progressUI.Hide();

        TimeRemaining.OnValueChanged += TickUI;

        if (!IsOpen)
            ToggleRenderer(false);
    }

    private void Update()
    {
        // Update interaction progress UI
        if (isPlayerNearby && !IsDrilling)
        {
            float progress = (float)clickTimes / clickAmount;
            progressUI.SetProgress(progress);
        }

        // Enable drill visuals while drilling
        if (IsDrilling && !transform.GetChild(1).GetComponent<Renderer>().enabled)
            ToggleRenderer(true);

        // Server handles countdown
        if (IsDrilling && IsServer && !IsOpen)
            TimeRemaining.Value -= Time.deltaTime;

        // Open door when finished
        if (IsOpen && !opened)
        {
            if (IsServer) _IsDrilling.Value = false;

            StartCoroutine(ToggleDoor(true));
            ToggleRenderer(false);

            opened = true;
        }
    }
    #endregion

    /// <summary>
    /// Updates UI timer text.
    /// </summary>
    private void TickUI(float previousValue, float newValue)
    {
        DrillText.text = $"{(int)newValue}";
    }

    /// <summary>
    /// Enables/disables drill visuals, colliders, and particles.
    /// </summary>
    private void ToggleRenderer(bool toggle)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (i == 0) continue; // Keep base visible

            Transform child = transform.GetChild(i);

            if (child.TryGetComponent(out Renderer r))
                r.enabled = toggle;

            if (child.TryGetComponent(out Collider c))
                c.enabled = toggle;

            if (child.TryGetComponent(out ParticleSystem p))
            {
                if (toggle) p.Play();
                else p.Stop();
            }
        }
    }

    /// <summary>
    /// Opens the vault door after drilling completes.
    /// </summary>
    private IEnumerator ToggleDoor(bool open)
    {
        if (!open) yield break;

        SoundManager.Instance.PlaySoundServerRpc(SoundType.DOOR_OPEN, transform.position);

        Quaternion start = Door.transform.rotation;
        Quaternion end = _doorOpen;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 0.5f;
            Door.transform.rotation = Quaternion.Lerp(start, end, t);
            yield return null;
        }

        Door.transform.rotation = end;
    }

    /// <summary>
    /// Starts drilling.
    /// </summary>
    private void PlaceDrill()
    {
        if (IsServer)
            _IsDrilling.Value = true;
        else
            ToggleDrillServerRpc(true);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ToggleDrillServerRpc(bool toggle)
    {
        _IsDrilling.Value = toggle;
    }

    #region Interaction

    /// <summary>
    /// Handles player interaction to place drill.
    /// </summary>
    public void Interact()
    {
        clickTimes++;

        if (clickTimes >= clickAmount)
            PlaceDrill();
    }

    public string InteractText() => string.Empty;

    /// <summary>
    /// Drill can only be used if not already drilling or opened.
    /// </summary>
    public bool CanInteract()
    {
        return !IsDrilling && !opened;
    }

    public void OnPlayerEnter()
    {
        isPlayerNearby = true;
        progressUI.Show();
        progressUI.SetProgress((float)clickTimes / clickAmount);
    }

    public void OnPlayerExit()
    {
        isPlayerNearby = false;
        progressUI.Hide();
    }

    #endregion
}