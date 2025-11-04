using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveUIManager : MonoBehaviour
{
    [Header("References")]
    public GameObject objectiveUIPrefab;
    public Transform objectiveContainer;

    private ObjectiveSystem objectiveSystem;
    private List<ObjectiveUI> objectiveUIList = new();

    private void Start()
    {
        objectiveSystem = ObjectiveSystem.Instance;

        InitializeObjectiveList();
    }

    private void Update()
    {
        if (objectiveSystem == null || objectiveUIList.Count == 0) return;

        int currentObjectiveIndex = objectiveSystem.CurrentObjectiveIndex;
        bool allObjectivesComplete = (currentObjectiveIndex == objectiveSystem.ObjectiveList.Count);

        for (int i = 0; i < objectiveUIList.Count; i++)
        {
            bool isComplete = i < currentObjectiveIndex;
            bool isCurrent = (i == currentObjectiveIndex) && !allObjectivesComplete;

            objectiveUIList[i].UpdateVisuals(isCurrent, isComplete);
        }
    }

    private void InitializeObjectiveList()
    {
        // Clear any old UI elements
        foreach (Transform child in objectiveContainer)
        {
            Destroy(child.gameObject);
        }
        objectiveUIList.Clear();

        // Create a UI element for each objective
        foreach (Objective objective in objectiveSystem.ObjectiveList)
        {
            GameObject objUI = Instantiate(objectiveUIPrefab, objectiveContainer);
            ObjectiveUI uiScript = objUI.GetComponent<ObjectiveUI>();

            if (uiScript != null)
            {
                uiScript.Setup(objective);
                objectiveUIList.Add(uiScript);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(objectiveContainer.GetComponent<RectTransform>());
        VerticalLayoutGroup group = objectiveContainer.GetComponent<VerticalLayoutGroup>();
        group.childControlHeight = true;
        group.childControlHeight = false;
    }
}