using System.IO;
using Unity.Netcode;
using UnityEngine;

public class NetPlayerList : NetworkBehaviour
{
    public NetworkList<NetString> usernames;

    private void Awake()
    {
        usernames = new NetworkList<NetString>();
    }

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
}
