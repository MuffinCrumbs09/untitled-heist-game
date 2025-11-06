using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class InteractionProgressUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Transform lookAtTarget;

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 5f;

    private Camera mainCamera;
    private float targetProgress = 0f;
    private float currentProgress = 0f;

    private void Awake()
    {
        progressFill.fillAmount = 0f;

        Hide();
    }

    private void SearchForPlayerCam()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            if (player.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId)
                mainCamera = player.GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (mainCamera == null) SearchForPlayerCam();

        if (canvas.enabled && mainCamera != null)
        {
            LookAtCamera();
            UpdateProgress();
        }
    }

    private void LookAtCamera()
    {
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(-directionToCamera);
    }

    private void UpdateProgress()
    {
        currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * smoothSpeed);

        if (progressFill != null)
            progressFill.fillAmount = currentProgress;
    }

    public void Show()
    {
        canvas.enabled = true;
    }

    public void Hide()
    {
        canvas.enabled = false;
        targetProgress = 0f;
        currentProgress = 0f;

        if (progressFill != null)
            progressFill.fillAmount = 0f;
    }

    public void SetProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
    }

    public void SetButtonText(string text)
    {
        if (buttonText != null)
            buttonText.text = text;
    }
}
