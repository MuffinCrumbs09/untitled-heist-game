using TMPro;
using UnityEngine;

public class StatsViewer : MonoBehaviour
{
    [Header("Settings - Text")]
    [SerializeField] private TMP_Text MoneyStole;
    [SerializeField] private TMP_Text CompleteHeists;
    [SerializeField] private TMP_Text TotalKills;


    private PlayerStats stats;

    private void Start()
    {
        stats = SaveManager.Instance.LoadGame();

        MoneyStole.text = $"Total stolen: ${stats.TotalMoneyStole}";
        CompleteHeists.text = $"Total Heists: {stats.TotalHeists}";
        TotalKills.text = $"Total Kills: {stats.TotalKills}";
    }
}
