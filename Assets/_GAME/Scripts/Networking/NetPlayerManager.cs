using System;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class NetPlayerManager : NetworkBehaviour
{
    public static NetPlayerManager Instance;
    public NetworkList<NetString> usernames;
    public NetworkList<NetPlayerState> playerStates;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;

        usernames = new NetworkList<NetString>();
        playerStates = new NetworkList<NetPlayerState>();
    }

    #region PlayerState
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerStateServerRpc(PlayerState newState, ServerRpcParams rpcParams = default)
    {
        ulong id = rpcParams.Receive.SenderClientId;
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


    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerStateServerRpc(PlayerState newState, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

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
    [ServerRpc(RequireOwnership = false)]
    public void AddUsernameServerRpc(string username)
    {
        usernames.Add(username);
        Debug.Log($"Added username '{username}' to shared list");
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
