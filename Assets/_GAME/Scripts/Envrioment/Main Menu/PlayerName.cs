using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerName : MonoBehaviour
{
    [Header("Settings - UI")]
    [SerializeField] private TMP_InputField txtInput;
    [SerializeField] private Button submitBttn;
    [Header("Settings - Misc")]
    [SerializeField] private bool isStartup = false;

    private RelayManager relay;
    private string filePath;

    private void Start()
    {
        relay = GetComponent<RelayManager>();
        filePath = Application.persistentDataPath + "/PlayerName.txt";
        submitBttn.onClick.AddListener(SetName);

        if (isStartup && File.Exists(filePath))
            relay.PickCanvas(0);
    }

    private void SetName()
    {
        string username = txtInput.text;
        if (username == null)
            return;

        using (var writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine(username);
        }

        if (isStartup)
            relay.PickCanvas(0);
        else
            txtInput.text = "Success!";
    }
}
