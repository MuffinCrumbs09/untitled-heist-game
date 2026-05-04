using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a loot object that players can interact with to gain money.
/// Uses a simple click-based interaction system with progress UI.
/// </summary>
public class Loot : NetworkBehaviour, IInteractable
{
    [Header("Settings")]
    [Tooltip("The amount of money this loot item is worth.")] public int LootValue = 10000;
    [Tooltip("The number of times the player must click to pick up the loot."), SerializeField] private int clickAmount = 1;

    [Header("UI")]
    [Tooltip("The objects Interact UI manager."), SerializeField] private InteractionProgressUI progressUI;

    private int clickTimes = 0;
    private bool isPlayerNearby = false;

    public override void OnNetworkSpawn()
    {
        // Initialize UI when object spawns on network
        progressUI.SetButtonText("E");
        progressUI.Hide();
    }

    #region Interface

    /// <summary>
    /// Determines if the loot can be interacted with. Required by IInteractable, always returns true for loot.
    /// </summary>
    public bool CanInteract() => true;

    /// <summary>
    /// Increments progress, updates UI, and if enough clicks have been made, calls server RPC to pick up loot.
    /// </summary>
    public void Interact()
    {
        clickTimes++;
        progressUI.SetProgress((float)clickTimes / clickAmount);

        if (clickTimes >= clickAmount)
            PickupLootServerRpc();
    }

    /// <summary>
    /// Text shown to the player (unused here). Required by IInteractable, returns empty string since loot uses world space UI.
    /// </summary>
    public string InteractText() => string.Empty;

    #endregion

    /// <summary>
    /// Runs on server to reward players and remove loot object.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void PickupLootServerRpc()
    {
        if (!IsSpawned) return;

        NetStore.Instance.ChangePayoutServerRpc(LootValue);
        NetworkObject.Despawn();
    }

    /// <summary>
    /// Called when player enters interaction range.
    /// </summary>
    public void OnPlayerEnter()
    {
        isPlayerNearby = true;
        progressUI.Show();
        progressUI.SetProgress((float)clickTimes / clickAmount);
    }

    /// <summary>
    /// Called when player leaves interaction range.
    /// </summary>
    public void OnPlayerExit()
    {
        isPlayerNearby = false;
        progressUI.Hide();
    }
}