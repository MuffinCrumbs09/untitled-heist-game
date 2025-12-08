using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class NetPlayerManager : NetworkBehaviour
{
    public static NetPlayerManager Instance;
    public NetworkList<NetString> usernames;
    public NetworkList<NetPlayerState> playerStates;

    // Server-Only Storage
    private Dictionary<ulong, string> _clientIdToName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;

        usernames = new NetworkList<NetString>();
        playerStates = new NetworkList<NetPlayerState>();
        _clientIdToName = new Dictionary<ulong, string>();
    }

    #region PlayerState
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetPlayerStateServerRpc(PlayerState newState, RpcParams rpc = default)
    {
        ulong id = rpc.Receive.SenderClientId;
        for (int i = 0; i < playerStates.Count; i++)
        {
            if (playerStates[i].clientID == id)
            {
                var temp = playerStates[i];
                temp.state = newState;
                playerStates[i] = temp;
            }
        }
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddPlayerStateServerRpc(PlayerState newState, RpcParams rpc = default)
    {
        ulong senderId = rpc.Receive.SenderClientId;

        // Check if this player has a state set up
        bool exists = false;
        for (int i = 0; i < playerStates.Count; i++)
        {
            if (playerStates[i].clientID == senderId)
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            playerStates.Add(new NetPlayerState
            {
                clientID = senderId,
                state = newState
            });

            Debug.Log("Added new client");
        }
        else
        {
            Debug.Log("Somehow, it already exists?");
        }
    }
    public PlayerState GetCurrentPlayerState(ulong clientID)
    {
        for (int i = 0; i < playerStates.Count; i++)
        {
            if (playerStates[i].clientID == clientID)
                return playerStates[i].state;
        }

        Debug.Log("Something happened, returning a default value:");
        return PlayerState.Error;
    }
    #endregion

    #region Usernames
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddUsernameServerRpc(string username, RpcParams rpc = default)
    {
        _clientIdToName[rpc.Receive.SenderClientId] = username;
        usernames.Add(username);

        Debug.Log($"Added username '{username}' to shared list");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RemoveUsernameServerRpc(ulong clientId)
    {
        if (_clientIdToName.TryGetValue(clientId, out string disconnectedName))
        {
            for (int i = 0; i < usernames.Count; i++)
                if (usernames[i] == disconnectedName)
                {
                    usernames.RemoveAt(i);
                    _clientIdToName.Remove(clientId);

                    Debug.Log($"Removed disconnected player: {disconnectedName}");
                }
        }
        else
        {
            Debug.LogWarning($"Attempted to remove ClientId {clientId} but key not found in dictionary.");
        }
    }

    public string GetAllUsernames()
    {
        string allPlayers = "";

        foreach (NetString username in usernames)
        {
            allPlayers += username.ToString() + "\n";
        }

        return allPlayers;
    }
    #endregion
}
