using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ObjectiveUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI objectiveNameText;
    public GameObject taskContainer;
    public GameObject taskUIPrefab;

    private Objective linkedObjective;
    private List<TaskUI> taskUIList = new();

    // Set up the UI element with data from the Objective
    public void Setup(Objective objective)
    {
        linkedObjective = objective;
        objectiveNameText.text = objective.objectiveName;

        // Clear previous tasks
        foreach (Transform child in taskContainer.transform)
        {
            Destroy(child.gameObject);
        }
        taskUIList.Clear();

        // Create UI for each task
        foreach (Task task in objective.tasks)
        {
            GameObject taskUIObj = Instantiate(taskUIPrefab, taskContainer.transform);
            TaskUI uiScript = taskUIObj.GetComponent<TaskUI>();
            if (uiScript != null)
            {
                uiScript.Setup(task);
                taskUIList.Add(uiScript);
            }
        }

        // Start collapsed
        taskContainer.SetActive(false);
        UpdateVisuals(false, false);
    }

    // Update the visual state based on system state
    public void UpdateVisuals(bool isCurrent, bool isComplete)
    {
        // Set completion style
        if (isComplete)
        {
            objectiveNameText.color = Color.green;
            objectiveNameText.fontStyle = FontStyles.Strikethrough;
        }
        else
        {
            objectiveNameText.color = isCurrent ? Color.white : new Color(0.8f, 0.8f, 0.8f); // Dim if not current
            objectiveNameText.fontStyle = FontStyles.Normal;
        }

        // Show/hide task list
        taskContainer.SetActive(isCurrent);

        // If this is the current objective, update its tasks
        if (isCurrent)
        {
            for (int i = 0; i < taskUIList.Count; i++)
            {
                if (linkedObjective.tasks.Count > i)
                {
                    taskUIList[i].UpdateVisuals(linkedObjective.tasks[i].isCompleted);
                }
            }
        }
    }
}