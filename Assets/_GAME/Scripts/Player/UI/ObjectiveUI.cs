using UnityEngine;
using UnityEngine.UI;
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

    public void Setup(Objective objective)
    {
        linkedObjective = objective;
        objectiveNameText.text = objective.objectiveName;

        ClearTasks();

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

        taskContainer.SetActive(false);
        UpdateVisuals(false, false);
    }

    public void UpdateVisuals(bool isCurrent, bool isComplete)
    {
        if (isComplete)
        {
            objectiveNameText.color = Color.green;
            objectiveNameText.fontStyle = FontStyles.Strikethrough;

            ClearTasks();
            if (taskContainer != null)
            {
                taskContainer.SetActive(false);
            }
        }
        else
        {
            objectiveNameText.color = isCurrent ? Color.white : new Color(0.8f, 0.8f, 0.8f);
            objectiveNameText.fontStyle = FontStyles.Normal;

            if (taskContainer != null)
            {
                taskContainer.SetActive(isCurrent);
            }

            if (isCurrent)
            {
                for (int i = 0; i < taskUIList.Count; i++)
                {
                    if (linkedObjective != null && linkedObjective.tasks.Count > i)
                    {
                        taskUIList[i].UpdateVisuals(linkedObjective.tasks[i].isCompleted);
                    }
                }
            }

            RefreshLayout();
        }
    }

    private void ClearTasks()
    {
        if (taskContainer == null) return;

        foreach (TaskUI taskUI in taskUIList)
        {
            if (taskUI != null)
            {
                Destroy(taskUI.gameObject);
            }
        }
        taskUIList.Clear();

        List<Transform> childrenToDestroy = new();
        foreach (Transform child in taskContainer.transform)
        {
            childrenToDestroy.Add(child);
        }

        foreach (Transform child in childrenToDestroy)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void RefreshLayout()
    {
        if (taskContainer != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(taskContainer.GetComponent<RectTransform>());
        }

        RectTransform objectiveRect = GetComponent<RectTransform>();
        if (objectiveRect != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(objectiveRect);
        }
    }
}
