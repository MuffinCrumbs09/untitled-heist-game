using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  ObjectiveComputerData.cs
//
//  ScriptableObject that describes a single computer-rewiring operation.
//  Consumed by ObjectiveComputerSetter at runtime.
//
//  Create via: Assets > Create > Map > Objective > Objective Computer
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Data asset that tells ObjectiveComputerSetter which computer to rewire,
/// which new MinigameTask to assign to it, and (optionally) a new hack time.
/// One asset = one rewire operation.
/// </summary>
[CreateAssetMenu(fileName = "New Objective Computer", menuName = "Map/Objective/Objective Computer")]
public class ObjectiveComputerData : ScriptableObject
{
    #region Data Fields

    [Header("Source Task (before rewire)")]
    [Tooltip("X = Objective index, Y = Task index of the computer task to be replaced. " +
             "This is used to locate the Computer object in the scene.")]
    public Vector2Int OriginalIndex;

    [Header("Target Task (after rewire)")]
    [Tooltip("X = Objective index, Y = Task index of the new MinigameTask to assign to that computer. " +
             "The rewire happens when this Objective becomes active.")]
    public Vector2Int NextIndex;

    [Header("Overrides")]
    [Tooltip("Optional: replaces the computer's hack-time duration (seconds). " +
             "Set to -1 to leave the existing value unchanged.")]
    public int NewHackTime = -1;

    #endregion
}