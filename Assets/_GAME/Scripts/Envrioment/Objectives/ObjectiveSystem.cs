using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ObjectiveSystem : MonoBehaviour
{
    public static ObjectiveSystem Instance;
    public List<Objective> ObjectiveList = new();

    [HideInInspector] public int CurrentObjectiveIndex { get; private set; } = 0;

    private PlayerStats stats;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;
    }

    private void Start()
    {
        stats = SaveManager.Instance.LoadGame();
    }

    private void Update()
    {
        if (CurrentObjectiveIndex == ObjectiveList.Count)
        {
            stats.TotalMoneyStole += NetStore.Instance.Payout.Value; 
            SaveManager.Instance.SaveGame(stats);

            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            NetworkManager.Singleton.Shutdown();
        }

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
