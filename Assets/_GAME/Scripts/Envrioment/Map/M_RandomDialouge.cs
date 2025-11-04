using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Map Dialouge", menuName = "Map Dialouge")]
public class M_RandomDialouge : ScriptableObject
{
    public List<string> ComputerDialouge = new();
}
