using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubtitleUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform subtitleContainer;
    [SerializeField] private GameObject subtitlePrefab;

    [Header("Settings")]
    [SerializeField] private int maxSubtitles = 5;

    private Queue<SubtitleUI> activeSubtitles = new();

    public void DisplaySubtitle(string username, string message, float duration)
    {
        if (activeSubtitles.Count >= maxSubtitles)
        {
            SubtitleUI oldestSubtitle = activeSubtitles.Dequeue();
            Destroy(oldestSubtitle.gameObject);
        }

        GameObject subtitleObj = Instantiate(subtitlePrefab, subtitleContainer);
        SubtitleUI subtitleUI = subtitleObj.GetComponent<SubtitleUI>();

        subtitleUI.Initialize(username, message, duration);
        activeSubtitles.Enqueue(subtitleUI);

        StartCoroutine(RemoveSubtitleAfterDuration(subtitleUI, duration));
    }
    private IEnumerator RemoveSubtitleAfterDuration(SubtitleUI subtitle, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (subtitle != null && subtitle.gameObject != null)
        {
            if (activeSubtitles.Contains(subtitle))
            {
                Queue<SubtitleUI> tempQueue = new Queue<SubtitleUI>();
                while (activeSubtitles.Count > 0)
                {
                    SubtitleUI current = activeSubtitles.Dequeue();
                    if (current != subtitle)
                    {
                        tempQueue.Enqueue(current);
                    }
                }
                activeSubtitles = tempQueue;
            }

            Destroy(subtitle.gameObject);
        }
    }
}
