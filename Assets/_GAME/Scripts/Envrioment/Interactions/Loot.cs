using System;
using Unity.Netcode;
using UnityEngine;

public class Loot : NetworkBehaviour, IInteractable
{
    [Header("Settings")]
    public int LootValue = 10000;
    [SerializeField] private int clickAmount = 1;

    [Header("UI")]
    [SerializeField] private InteractionProgressUI progressUI;

    private int clickTimes = 0;
    private bool isPlayerNearby = false;

    private void Start()
    {
        progressUI.SetButtonText("E");
        progressUI.Hide();
    }

    private void Update()
    {
        if (isPlayerNearby)
        {
            float progress = (float)clickTimes / clickAmount;
            progressUI.SetProgress(progress);
        }
    }

    #region Interface
    // Unused
    public bool CanInteract()
    {
        return true;
    }

    public void Interact()
    {
        clickTimes++;

        if (clickTimes >= clickAmount)
            PickupLoot();
    }

    public string InteractText()
    {
        return string.Empty;
    }
    #endregion

    private void PickupLoot()
    {
        progressUI.Hide();
        PickupLootServerRpc();
        
        NetworkObject.Despawn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PickupLootServerRpc()
    {
        NetStore.Instance.ChangePayoutServerRpc(LootValue);
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
}