using TMPro;
using UnityEngine;

public class SubtitleUI : MonoBehaviour
{
    [SerializeField] private TMP_Text subtitleText;

    public void Initialize(string username, string message, float duration)
    {
        string cleanUsername = username?.Trim() ?? "Unknown";
        string cleanMessage = message?.Trim() ?? "";

        subtitleText.text = $"{cleanUsername} : {cleanMessage}";
    }
}
