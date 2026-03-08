using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManger : MonoBehaviour
{
    public List<string> ObjectiveList = new();
    public TMP_Text ObjectiveText;

    private int curObjectiveIndex;

    private void Start()
    {
        UpdateText();
    }

    private void UpdateText()
    {
        ObjectiveText.text = ObjectiveList[curObjectiveIndex];
    }

    public void ObjectiveComplete()
    {
        curObjectiveIndex++;
        UpdateText();
    }
}
