using TMPro;
using UnityEngine;

public class StatsViewer : MonoBehaviour
{
    [SerializeField] private TMP_Text UIText;
    private PlayerStats stats;

    private void Start()
    {
        stats = SaveManager.Instance.LoadGame();

        UIText.text = $"${stats.TotalMoneyStole}";  
    }
}
