using System.IO;
using Unity.Netcode;
using UnityEngine;

public class NetStore : NetworkBehaviour
{
    // Where global game stats will be stored (Cash Secured, Total kills etc)
    public static NetStore Instance;
    public NetworkVariable<int> MaxPayout = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> Payout = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangePayoutServerRpc(int toChange)
    {
        Payout.Value += toChange;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMaxPayoutServerRpc(int max)
    {
        MaxPayout.Value = max;
    }
}
