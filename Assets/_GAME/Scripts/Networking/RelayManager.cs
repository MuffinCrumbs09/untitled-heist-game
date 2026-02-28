using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{
    #region Variables
    [Header("UI")]
    [SerializeField] private Canvas[] Canvas;
    [Space(20), Header("UI - Buttons")]
    [SerializeField] private Button CreateLobby;
    [SerializeField] private Button JoinLobby;
    [SerializeField] private Button Back;
    [SerializeField] private Button QuitLobbyB;
    [SerializeField] private Button QuitGameB;
    [SerializeField] private Button CodeBttn;
    [SerializeField] private Button StartGameB;
    [SerializeField] private Button OptionsB;
    [SerializeField] private Button StatsB;
    [SerializeField] private Button StatsBackB;
    [Space(20), Header("UI - TMP")]
    [SerializeField] private TMP_Text CodeText;
    [SerializeField] private TMP_Text PlayerListTxt;
    [SerializeField] private TMP_InputField JoinInput;

    [Space(20), Header("Misc")]
    [SerializeField] private List<GameObject> Players = new();
    [SerializeField] private Animator _eraseAnim;
    [SerializeField] private Animator _cameraAnim;

    private string joinCode;
    private string filePath;
    #endregion

    #region Unity Functions
    private void OnDisable()
    {
        NetPlayerManager.Instance.playerData.OnListChanged -= OnPlayerDataChanged;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnConnectionEvent -= ConnectionEvent;
    }

    private async void Start()
    {
        // Connect to unity services
        await UnityServices.InitializeAsync();

        // Sign in if you arent.
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        NetworkManager.Singleton.OnConnectionEvent += ConnectionEvent;
        NetPlayerManager.Instance.playerData.OnListChanged += OnPlayerDataChanged;

        // Button Assignment
        CreateLobby.onClick.AddListener(StartRelay);
        Back.onClick.AddListener(() => CanvasManager.Instance.PickCanvas(CurrentCanvas.MainMenu));
        QuitLobbyB.onClick.AddListener(QuitLobby);
        QuitGameB.onClick.AddListener(QuitGame);
        JoinLobby.onClick.AddListener(() => JoinRelay(JoinInput.text));
        StartGameB.onClick.AddListener(StartGame);
        CodeBttn.onClick.AddListener(CopyCode);
        OptionsB.onClick.AddListener(() => CanvasManager.Instance.PickCanvas(CurrentCanvas.Options));
        StatsB.onClick.AddListener(ToggleCam);
        StatsBackB.onClick.AddListener(ToggleCam);

        filePath = Application.persistentDataPath + "/PlayerName.txt";
    }
    #endregion

    #region Functions
    private void CopyCode()
    {
        GUIUtility.systemCopyBuffer = CodeText.text;
    }

    private void ToggleCam()
    {
        _cameraAnim.SetTrigger("Toggle");
    }

    //Loads the game scene
    public void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("MicroBank", LoadSceneMode.Single);
    }
    private void QuitLobby()
    {
        // Quits the lobby
        NetworkManager.Singleton.Shutdown();

        // Delete the network dependencies
        Destroy(NetworkManager.Singleton.gameObject);
        Destroy(NetPlayerManager.Instance.gameObject);

        // Main Menu (Loading scene creates fresh network dependencies)
        SceneManager.LoadScene(0);
    }

    private void QuitGame()
    {
        // Leave the server then quit the game
        NetworkManager.Singleton.Shutdown();
        Application.Quit();
    }
    #endregion

    #region Relay
    private async void StartRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3); // 3 peers, 1 host

        joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId); // get the join code

        // Create and set relay server data
        var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        // Start server as host
        NetworkManager.Singleton.StartHost();

        // Activate the start button, since this is the host
        StartGameB.gameObject.SetActive(true);

        // Switch Canvas
        CanvasManager.Instance.PickCanvas(CurrentCanvas.InLobby);
    }

    private async void JoinRelay(string joinCode)
    {
        // Grab the lobby allocation from join code and set relay 
        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        //  Join lobby as client
        NetworkManager.Singleton.StartClient();

        // Show join code on screen
        this.joinCode = joinCode;


        // Switch Canvas
        CanvasManager.Instance.PickCanvas(CurrentCanvas.Connecting);
    }
    #endregion

    #region Events
    private void ConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        // Client Connected
        if (data.EventType == Unity.Netcode.ConnectionEvent.ClientConnected)
        {
            if (data.ClientId != manager.LocalClientId) return;

            CodeText.text = joinCode; // Display join code
            string playerName = File.ReadAllText(filePath);

            // Add player to the net player manager
            NetPlayerManager.Instance.AddNewPlayerDataServerRpc(playerName);
            CanvasManager.Instance.PickCanvas(CurrentCanvas.InLobby);
        }
        // Client Disconnected
        else if (data.EventType == Unity.Netcode.ConnectionEvent.ClientDisconnected)
        {
            if (manager.IsClient && !manager.IsHost)
            {
                Debug.Log("Client detected disconnect - quitting lobby");
                QuitLobby();
                return;
            }

            if (manager.IsServer && manager.IsListening)
                NetPlayerManager.Instance.RemovePlayerDataByIDServerRpc(data.ClientId);
        }
    }

    private void OnPlayerDataChanged(NetworkListEvent<NetPlayerData> changeEvent)
    {
        // Show Players
        foreach (GameObject obj in Players)
            obj.SetActive(false);

        int playerCount = NetPlayerManager.Instance.playerData.Count;

        for (int x = 0; x <= playerCount - 1; x++)
        {
            Players[x].SetActive(true);
        }

        // Show Usernames
        List<string> name = new();

        foreach (NetPlayerData data in NetPlayerManager.Instance.playerData)
        {
            name.Add(data.USERNAME);
        }

        string finalString = string.Join("\n", name);
        PlayerListTxt.text = finalString;
    }
    #endregion
}
