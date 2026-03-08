using UnityEngine;

public class LootTask : Task
{
    [Header("Loot Settings")]
    public int maxPayoutPercent;

    public override void UpdateTask()
    {
        int targetPayout = (NetStore.Instance.MaxPayout.Value * maxPayoutPercent) / 100;

        if (NetStore.Instance.Payout.Value >= targetPayout)
            isCompleted = true;
    }
}
