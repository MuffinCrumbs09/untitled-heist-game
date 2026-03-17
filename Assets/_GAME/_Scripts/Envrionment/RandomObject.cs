using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class RandomObject : NetworkBehaviour
{
    public NetworkVariable<bool> isSpawned = new(false);
    #region Inspector Fields

    #endregion

    public override void OnNetworkSpawn()
    {
        isSpawned.OnValueChanged += UpdateState;
        gameObject.SetActive(false);
    }

    [Rpc(SendTo.Server)]
    public void ChangeStateRpc(bool state)
    {
        if(isSpawned.Value == state) return;
        isSpawned.Value = state;
    }

    private void UpdateState(bool previous, bool current)
    {
        gameObject.SetActive(current);
    }
}