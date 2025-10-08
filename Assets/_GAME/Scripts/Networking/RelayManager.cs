using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{
    [SerializeField] private Canvas[] Canvas;
    [SerializeField] private Button CreateLobby;
    [SerializeField] private Button JoinLobby;
    [SerializeField] private Button Back;
    [SerializeField] private Button QuitLobbyB;
    [SerializeField] private Button QuitGameB;
    [SerializeField] private TMP_Text CodeText;
    [SerializeField] private TMP_Text PlayerListTxt;
    [SerializeField] private Button StartGameB;
    [SerializeField] private TMP_InputField JoinInput;
    [SerializeField] private NetPlayerList PlayerList;
    private string joinCode;
    private string filePath;

    private async void Start()
    {
        // Connect to unity services
        await UnityServices.InitializeAsync();

        // Sign in if you arent.
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        CreateLobby.onClick.AddListener(StartRelay);
        //Back.onClick.AddListener(BackToMenu);
        QuitLobbyB.onClick.AddListener(QuitLobby);
        QuitGameB.onClick.AddListener(QuitGame);
        JoinLobby.onClick.AddListener(() => JoinRelay(JoinInput.text));
        StartGameB.onClick.AddListener(StartGame);

        filePath = Application.persistentDataPath + "/PlayerName.txt";
    }

    private void Update()
    {
        if (PlayerListTxt.text == PlayerList.GetAllUsernames())
            return;

        PlayerListTxt.text = PlayerList.GetAllUsernames();
    }

    #region Functions
    private void SwapCanvas(int before, int after)
    {
        Canvas[before].enabled = false;
        Canvas[after].enabled = true;
    }

    public void BackToMenu()
    {
        for (int x = 0; x < Canvas.Length; x++)
        {
            Debug.Log(x);
            if (x > 0)
                Canvas[x].enabled = false;
            else
                Canvas[x].enabled = true;
        }
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
        SwapCanvas(0, 1);
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
        SwapCanvas(0, 1);
    }

    //Loads the game scene
    public void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("Test Map", LoadSceneMode.Single);
    }

    private IEnumerator WaitandAddUser()
    {
        while (PlayerList == null || !PlayerList.IsSpawned)
            yield return null;

        string playerName = File.ReadAllText(filePath);
        PlayerList.AddUsernameServerRpc(playerName);
    }

    private void QuitLobby()
    {
        // Quits the lobby
        NetworkManager.Singleton.Shutdown();

        // Delete the network manager
        Destroy(NetworkManager.Singleton.gameObject);
        Destroy(PlayerList.gameObject);

        // Main Menu (Create a new network manager)
        SceneManager.LoadScene(0);
    }

    private void QuitGame()
    {
        NetworkManager.Singleton.Shutdown();
        Application.Quit();
    }
}
