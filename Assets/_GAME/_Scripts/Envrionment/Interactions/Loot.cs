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

    public override void OnNetworkSpawn()
    {
        progressUI.SetButtonText("E");
        progressUI.Hide();
    }

    #region Interface
    public bool CanInteract() => true;

    public void Interact()
    {
        clickTimes++;
        progressUI.SetProgress((float)clickTimes / clickAmount);

        if (clickTimes >= clickAmount)
            PickupLootServerRpc();
    }

    public string InteractText() => string.Empty;
    #endregion

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void PickupLootServerRpc()
    {
        if (!IsSpawned) return;

        NetStore.Instance.ChangePayoutServerRpc(LootValue);
        NetworkObject.Despawn();
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