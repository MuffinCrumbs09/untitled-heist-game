using System.Collections.Generic;
using UnityEngine;

public class ObjectiveSystem : MonoBehaviour
{
    public static ObjectiveSystem Instance;
    public List<Objective> ObjectiveList = new();

    [HideInInspector] public int CurrentObjectiveIndex { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;
    }

    private void Update()
    {
        if (CurrentObjectiveIndex == ObjectiveList.Count) return;

        ObjectiveList[CurrentObjectiveIndex].UpdateObjective();

        if (ObjectiveList[CurrentObjectiveIndex].IsCompleted())
        {
            Debug.Log("Completed Objective " + CurrentObjectiveIndex);
            CurrentObjectiveIndex++;
        }
    }
    
    public Objective GetCurObjective()
    {
        return CurrentObjectiveIndex == ObjectiveList.Count ? ObjectiveList[CurrentObjectiveIndex - 1] : ObjectiveList[CurrentObjectiveIndex];
    }
}
