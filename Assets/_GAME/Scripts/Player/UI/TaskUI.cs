using UnityEngine;
using TMPro;

public class TaskUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI taskNameText;

    // Set up the task's text
    public void Setup(Task task)
    {
        taskNameText.text = task.taskName;
        UpdateVisuals(task.isCompleted);
    }

    // Update visual state (strikethrough for completion)
    public void UpdateVisuals(bool isComplete)
    {
        if (isComplete)
        {
            taskNameText.color = new Color(0.7f, 0.7f, 0.7f); // Grayed out
            taskNameText.fontStyle = FontStyles.Strikethrough;
        }
        else
        {
            taskNameText.color = Color.white;
            taskNameText.fontStyle = FontStyles.Normal;
        }
    }
}