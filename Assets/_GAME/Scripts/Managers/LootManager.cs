using UnityEngine;

public class LootManager : MonoBehaviour
{
    public static LootManager Instance;

    public int CurrentLootCount { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;
    }

    public void AddLoot() => CurrentLootCount++;
}
