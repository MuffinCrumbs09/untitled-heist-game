using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that holds a pool of random dialogue lines, currently only for wrong computer hacks
/// Create new dialogue sets via: Assets > Create > Map > Map Dialogue
/// </summary>
[CreateAssetMenu(fileName = "New Map Dialouge", menuName = "Map/Map Dialouge")]
public class M_RandomDialouge : ScriptableObject
{
    // The pool of dialogue lines to choose from when a computer hack fails.
    // A random line is selected at runtime from this list.
    public List<string> ComputerDialouge = new();
}