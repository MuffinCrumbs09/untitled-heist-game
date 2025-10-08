using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerName : MonoBehaviour
{
    [SerializeField] private TMP_InputField txtInput;
    [SerializeField] private Button submitBttn;
    private RelayManager relay;
    private string filePath;

    private void Start()
    {
        relay = GetComponent<RelayManager>();
        filePath = Application.persistentDataPath + "/PlayerName.txt";
        submitBttn.onClick.AddListener(SetName);

        if (File.Exists(filePath))
            relay.BackToMenu();
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

        relay.BackToMenu();
    }
}
