using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the input and storage of the player's username to a local text file.
/// </summary>
public class PlayerName : MonoBehaviour
{
    #region Variables

    [Header("Settings - UI")]
    [SerializeField] 
    [Tooltip("Input field where the player types their name.")]
    private TMP_InputField txtInput;

    [SerializeField] 
    [Tooltip("Button used to confirm the name change.")]
    private Button submitBttn;

    [Header("Settings - Misc")]
    [SerializeField] 
    [Tooltip("If true, the script will check for an existing name and skip to the main menu on start.")]
    private bool isStartup = false;

    private string filePath;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Sets up the file path and button listeners. Automatically redirects to Main Menu if a name exists on startup.
    /// </summary>
    private void Start()
    {
        filePath = Application.persistentDataPath + "/PlayerName.txt";
        submitBttn.onClick.AddListener(SetName);

        if (isStartup && File.Exists(filePath))
            CanvasManager.Instance.PickCanvas(CurrentCanvas.MainMenu);
    }

    #endregion

    #region Logic

    /// <summary>
    /// Saves the input field text to a local file and handles UI feedback.
    /// </summary>
    private void SetName()
    {
        string username = txtInput.text;
        if (string.IsNullOrEmpty(username)) return;

        using (var writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine(username);
        }

        if (isStartup)
            CanvasManager.Instance.PickCanvas(CurrentCanvas.MainMenu);
        else
            txtInput.text = "Success!";
    }

    #endregion
}