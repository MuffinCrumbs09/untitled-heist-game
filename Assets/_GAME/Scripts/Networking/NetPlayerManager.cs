using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class NetPlayerManager : NetworkBehaviour
{
    public static NetPlayerManager Instance;

    public NetworkList<NetPlayerData> playerData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;

        playerData = new();
    }

    #region Write
    [Rpc(SendTo.Server)]
    public void AddNewPlayerDataServerRpc(string username, RpcParams rpc = default)
    {
        ulong id = rpc.Receive.SenderClientId;
        playerData.Add(new NetPlayerData(username, id));
    }

    [Rpc(SendTo.Server)]
    public void RemovePlayerDataByIDServerRpc(ulong targetID)
    {
        for (int i = 0; i < playerData.Count; i++)
        {
            ulong id = playerData[i].CLIENTID;
            if (id.Equals(targetID))
                playerData.RemoveAt(i);
        }
    }

    [Rpc(SendTo.Server)]
    public void SetPlayerStateServerRpc(PlayerState newState, RpcParams rpc = default)
    {
        ulong targetID = rpc.Receive.SenderClientId;

        for (int i = 0; i < playerData.Count; i++)
        {
            var temp = playerData[i];
            if (temp.CLIENTID.Equals(targetID))
            {
                temp.STATE = newState;
                playerData[i] = temp;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void AddPlayerKillServerRpc(RpcParams rpc = default)
    {
        ulong targetID = rpc.Receive.SenderClientId;

        for (int i = 0; i < playerData.Count; i++)
        {
            var temp = playerData[i];
            if (temp.CLIENTID.Equals(targetID))
            {
                temp.KILLS++;
                playerData[i] = temp;
            }
        }
    }
    #endregion

    #region Read
    public PlayerState GetCurrentPlayerState(ulong targetID)
    {
        for (int i = 0; i < playerData.Count; i++)
        {
            var temp = playerData[i];
            if (temp.CLIENTID == targetID)
                return temp.STATE;
        }

        return PlayerState.Error;
    }

    public int GetLocalPlayersKills()
    {
        ulong targetID = NetworkManager.Singleton.LocalClientId;

        for (int i = 0; i < playerData.Count; i++)
        {
            var temp = playerData[i];
            if (temp.CLIENTID == targetID)
                return temp.KILLS;
        }

        return 0;
    }
    #endregion
}
