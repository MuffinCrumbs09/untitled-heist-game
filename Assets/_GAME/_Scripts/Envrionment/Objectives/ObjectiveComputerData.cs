using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Objective Hint", menuName = "Map/Objective/Objective Computer")]
public class ObjectiveComputerData : ScriptableObject
{
    [Header("Objective Data")]
    [Tooltip("The Objective Index and Task Index of the original objective")]
    public Vector2Int OriginalIndex;
    [Tooltip("The Objective Index and Task Index of the next objective")]
    public Vector2Int NextIndex;
    [Tooltip("How long the new hack should be, -1 = no change")]
    public int NewHackTime = -1;
}