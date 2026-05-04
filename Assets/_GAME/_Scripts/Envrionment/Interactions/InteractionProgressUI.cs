using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// Handles world-space interaction UI with smooth progress bar and camera-facing behavior.
/// </summary>
public class InteractionProgressUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The canvas that contains the interaction UI."), SerializeField] private Canvas canvas;
    [Tooltip("The image component representing the progress fill."), SerializeField] private Image progressFill;
    [Tooltip("The text component for the interaction button."), SerializeField] private TextMeshProUGUI buttonText;

    [Header("Settings")]
    [Tooltip("The speed at which the progress bar animates."), SerializeField] private float smoothSpeed = 5f;

    private Camera mainCamera;

    private float targetProgress = 0f;
    private float currentProgress = 0f;

    #region Unity LifeCycle
    private void Awake()
    {
        progressFill.fillAmount = 0f;
        Hide();
    }

    private void Update()
    {
        if (mainCamera == null)
            SearchForPlayerCam();

        if (canvas.enabled && mainCamera != null)
        {
            LookAtCamera();
            UpdateProgress();
        }
    }
    #endregion

    /// <summary>
    /// Finds the local player's camera in a networked scene.
    /// </summary>
    private void SearchForPlayerCam()
    {
        GameObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject?.gameObject;
        if (localPlayer != null)
            mainCamera = localPlayer.GetComponentInChildren<Camera>();
    }

    /// <summary>
    /// Rotates UI to always face the camera.
    /// </summary>
    private void LookAtCamera()
    {
        Vector3 dir = mainCamera.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(-dir);
    }

    /// <summary>
    /// Smoothly animates progress bar.
    /// </summary>
    private void UpdateProgress()
    {
        currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * smoothSpeed);
        progressFill.fillAmount = currentProgress;
    }

    public void Show() => canvas.enabled = true;

    /// <summary>
    /// Hides UI and resets progress.
    /// </summary>
    public void Hide()
    {
        if (canvas == null) return;

        canvas.enabled = false;
        targetProgress = 0f;
        currentProgress = 0f;
        progressFill.fillAmount = 0f;
    }

    /// <summary>
    /// Sets target progress (0–1).
    /// </summary>
    public void SetProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
    }

    /// <summary>
    /// Updates interaction button label. Will be useful when controller support and custom keybinds are implemented.
    /// </summary>
    public void SetButtonText(string text)
    {
        if (buttonText != null)
            buttonText.text = text;
    }
}