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

    private string filePath;

    private void Start()
    {
        filePath = Application.persistentDataPath + "/PlayerName.txt";
        submitBttn.onClick.AddListener(SetName);

        if (isStartup && File.Exists(filePath))
            CanvasManager.Instance.PickCanvas(CurrentCanvas.MainMenu);
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
            CanvasManager.Instance.PickCanvas(CurrentCanvas.MainMenu);
        else
            txtInput.text = "Success!";
    }
}
