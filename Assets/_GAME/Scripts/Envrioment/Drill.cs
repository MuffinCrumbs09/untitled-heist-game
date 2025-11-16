using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AppUI.UI;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Drill : NetworkBehaviour, IInteractable
{
    [Header("Drill - Settings")]
    [SerializeField] private float TimeToDrill;
    [SerializeField] private GameObject Door;
    [SerializeField] private Vector3 DoorOpen;
    [SerializeField] private TMP_Text DrillText;
    [Header("Interaction - Settings")]
    [SerializeField] private int clickAmount = 1;
    [SerializeField] private InteractionProgressUI progressUI;

    [Header("Network Variables")]
    public NetworkVariable<float> TimeRemaining = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> _IsDrilling = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsJammed = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool IsOpen => TimeRemaining.Value <= 0;
    private bool IsDrilling => _IsDrilling.Value;
    private bool opened;

    private Quaternion _doorOpen;

    private int clickTimes = 0;
    private bool isPlayerNearby;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            TimeRemaining.Value = TimeToDrill;
    }

    private void Start()
    {
        _doorOpen = Quaternion.Euler(DoorOpen.x, DoorOpen.y, DoorOpen.z);
        DrillText.text = TimeToDrill.ToString();

        progressUI.SetButtonText("E");
        progressUI.Hide();

        TimeRemaining.OnValueChanged += TickUI;

        if (!IsOpen)
            ToggleRenderer(false);
    }

    private void Update()
    {
        if (isPlayerNearby && !IsDrilling)
        {
            float progress = (float)clickTimes / clickAmount;
            progressUI.SetProgress(progress);
        }

        if (IsDrilling && !transform.GetChild(1).GetComponent<Renderer>().enabled)
            ToggleRenderer(true);

        if (IsDrilling && IsServer && !IsOpen)
        {
            TimeRemaining.Value -= Time.deltaTime;
        }

        if (IsOpen && !opened)
        {
            if (IsServer) _IsDrilling.Value = false;
            StartCoroutine(ToggleDoor(IsOpen));
            ToggleRenderer(false);
            opened = true;
        }
    }

    private void TickUI(float previousValue, float newValue)
    {
        DrillText.text = $"{(int)newValue}";

    }

    private void ToggleRenderer(bool toggle)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (i == 0) continue; // skip first so player can interact. Will update in future
            if (child.TryGetComponent(out Renderer render))
                render.enabled = toggle;
            if (child.TryGetComponent(out Collider col))
                col.enabled = toggle;
            if (child.TryGetComponent(out ParticleSystem particle))
                if (toggle) particle.Play();
                else particle.Stop();

        }
    }

    private IEnumerator ToggleDoor(bool open)
    {
        if (!open)
            yield break;


        SoundType type = SoundType.DOOR_OPEN;
        SoundManager.Instance.PlaySoundServerRpc(type, transform.position);

        Quaternion startRot = Door.transform.rotation;
        Quaternion endRot = _doorOpen;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * .5f;
            Door.transform.rotation = Quaternion.Lerp(startRot, endRot, elapsed);
            yield return null;
        }

        Door.transform.rotation = endRot;
    }

    private void PlaceDrill()
    {
        if (IsServer)
            _IsDrilling.Value = true;
        else
            ToggleDrillServerRpc(true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleDrillServerRpc(bool toggle)
    {
        _IsDrilling.Value = toggle;
    }

    #region Interaction
    public void Interact()
    {
        clickTimes++;

        if (clickTimes >= clickAmount)
            PlaceDrill();
    }

    public string InteractText()
    {
        return string.Empty;
    }

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
