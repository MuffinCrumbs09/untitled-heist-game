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
    private float _playerCount;

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnConnectionEvent -= Connected;
    }

    private async void Start()
    {
        // Connect to unity services
        await UnityServices.InitializeAsync();

        // Sign in if you arent.
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        NetworkManager.Singleton.OnConnectionEvent += Connected;

        CreateLobby.onClick.AddListener(StartRelay);
        Back.onClick.AddListener(() => PickCanvas(0));
        QuitLobbyB.onClick.AddListener(QuitLobby);
        QuitGameB.onClick.AddListener(QuitGame);
        JoinLobby.onClick.AddListener(() => JoinRelay(JoinInput.text));
        StartGameB.onClick.AddListener(StartGame);
        CodeBttn.onClick.AddListener(CopyCode);
        OptionsB.onClick.AddListener(() => PickCanvas(4));
        StatsB.onClick.AddListener(ToggleCam);
        StatsBackB.onClick.AddListener(ToggleCam);

        filePath = Application.persistentDataPath + "/PlayerName.txt";
    }

    private void Update()
    {
        if (PlayerListTxt.text != NetPlayerManager.Instance.GetAllUsernames())
            PlayerListTxt.text = NetPlayerManager.Instance.GetAllUsernames();

        if (_playerCount != Players.Count)
            ShowPlayers();
    }

    #region Functions
    // 0 = Main Menu, 1 = In lobby, 2 = Pick username, 3 = Connecting, 4 = Options
    public void PickCanvas(int canvas)
    {
        _eraseAnim.SetTrigger("Erase");

        StartCoroutine(PickCanvasRoutine(canvas));
    }

    private IEnumerator PickCanvasRoutine(int canvas)
    {
        yield return new WaitForSeconds(.5f);

        for (int x = 0; x < Canvas.Length; x++)
        {
            Canvas[x].enabled = false;
        }

        yield return new WaitForSeconds(1f);

        for (int x = 0; x < Canvas.Length; x++)
        {
            Canvas[x].enabled = (x == canvas);
        }
    }

    private void CopyCode()
    {
        GUIUtility.systemCopyBuffer = CodeText.text;
    }

    private void Connected(NetworkManager manager, ConnectionEventData eventData)
    {
        if (eventData.EventType == ConnectionEvent.ClientConnected)
            PickCanvas(1);
    }

    private void ToggleCam()
    {
        _cameraAnim.SetTrigger("Toggle");
    }
    #endregion

    private async void StartRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3); // 3 peers, 1 host

        joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId); // get the join code

        // Show join code on screen
        CodeText.text = joinCode;

        // Create and set relay server data
        var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        // Start server as host
        NetworkManager.Singleton.StartHost();

        // Activate the start button, since this is the host
        StartGameB.gameObject.SetActive(true);

        StartCoroutine(WaitandAddUser());

        // Switch Canvas
        PickCanvas(1);
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
        CodeText.text = joinCode;

        StartCoroutine(WaitandAddUser());

        // Switch Canvas
        PickCanvas(3);
    }

    //Loads the game scene
    public void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("Prototype Map", LoadSceneMode.Single);
    }

    private IEnumerator WaitandAddUser()
    {
        // Wait unitl the player manager has been spawned
        while (NetPlayerManager.Instance == null || !NetPlayerManager.Instance.IsSpawned)
            yield return null;

        string playerName = File.ReadAllText(filePath);

        // Add player to the net player manager
        NetPlayerManager.Instance.AddUsernameServerRpc(playerName);
        NetPlayerManager.Instance.AddPlayerStateServerRpc(PlayerState.MaskOff);
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

    private void ShowPlayers()
    {
        int playerCount = NetPlayerManager.Instance.usernames.Count;
        _playerCount = 0;

        for (int x = 0; x <= playerCount - 1; x++)
        {
            Players[x].SetActive(true);
            _playerCount++;
        }


    }
}
