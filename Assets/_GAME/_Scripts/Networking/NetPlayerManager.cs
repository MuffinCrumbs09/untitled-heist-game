using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Player, $"Player '{temp.USERNAME}' changed state to '{newState}'.", this);
#endif
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

    /// <summary>
    /// Credits a kill to the player with the given clientId. 
    /// Must only be called server-side (e.g. from EnemyHealth.Die).
    /// </summary>
    public void AddKillForPlayer(ulong clientId)
    {
        for (int i = 0; i < playerData.Count; i++)
        {
            var temp = playerData[i];
            if (temp.CLIENTID == clientId)
            {
                temp.KILLS++;
                playerData[i] = temp;

#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Player, $"Kill credited to '{temp.USERNAME}' (id {clientId}).", this);
#endif
                return;
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

    #region Save
    public void SavePlayerDataToDesktop()
    {
        if (playerData == null || playerData.Count == 0)
        {
#if UNITY_EDITOR
            LoggerEvent.LogWarning(LogPrefix.Player, "NetPlayerManager: No player data to save.", this);
#endif
            return;
        }

        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filePath = Path.Combine(desktopPath, $"PlayerData_{timestamp}.txt");

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("=== NetPlayerManager - Player Data Export ===");
            writer.WriteLine($"Exported: {DateTime.Now}");
            writer.WriteLine($"Total Players: {playerData.Count}");
            writer.WriteLine(new string('-', 40));

            for (int i = 0; i < playerData.Count; i++)
            {
                var data = playerData[i];
                writer.WriteLine($"[Player {i + 1}]");
                writer.WriteLine($"  Username : {data.USERNAME}");
                writer.WriteLine($"  Client ID: {data.CLIENTID}");
                writer.WriteLine($"  State    : {data.STATE}");
                writer.WriteLine($"  Kills    : {data.KILLS}");
                writer.WriteLine($"  Skills    : {data.SKILLS}");
                writer.WriteLine();
            }
        }

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Player, $"Player data saved to '{filePath}'.", this);
#endif
    }

    [Rpc(SendTo.Server)]
    public void SetPlayerSkillsServerRpc(int skillMask, RpcParams rpc = default)
    {
        ulong targetID = rpc.Receive.SenderClientId;

        for (int i = 0; i < playerData.Count; i++)
        {
            var temp = playerData[i];
            if (temp.CLIENTID.Equals(targetID))
            {
                temp.SKILLS = skillMask;
                playerData[i] = temp;
                return;
            }
        }
    }
    #endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(NetPlayerManager))]
public class NetPlayerManagerEditor : Editor
{
    [ContextMenu("Save Player Data to Desktop")]
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    [MenuItem("CONTEXT/NetPlayerManager/Save Player Data to Desktop")]
    private static void SavePlayerData(MenuCommand command)
    {
        NetPlayerManager manager = (NetPlayerManager)command.context;

        if (manager == null)
        {
            Debug.LogError("[NetPlayerManager] Could not find component.");
            return;
        }

        manager.SavePlayerDataToDesktop();
    }
}
#endif