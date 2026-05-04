using UnityEngine;
using UnityEditor;

// ─────────────────────────────────────────────────────────────────────────────
//  ChildGiver.cs
//
//  Editor window that instantiates a chosen prefab as a child of every
//  currently selected GameObject in the scene.
//
//  Open via: Tools > Custom > Child Giver
//
//  Usage:
//    1. Select one or more GameObjects in the Hierarchy.
//    2. Assign a prefab in the "Prefab To Add" field.
//    3. Toggle which local transform values should be reset to default.
//    4. Click "Add Prefab To Selected".
//
//  The operation is fully undoable (Ctrl+Z). All instances created in one
//  click are collapsed into a single undo step.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Editor utility window that adds a prefab as a child object to each
/// selected GameObject, with optional local transform resets and full undo support.
/// </summary>
public class ChildGiver : EditorWindow
{
    #region Private State

    // The prefab to instantiate. Serialized so it survives assembly reloads.
    [SerializeField] private GameObject prefabToAdd;

    // Controls which transform components are zeroed out after parenting.
    private bool resetLocalPosition = true;
    private bool resetLocalRotation = true;
    private bool resetLocalScale = true;

    #endregion

    #region Menu Item

    /// <summary>Opens the Child Giver window from the Unity menu bar.</summary>
    [MenuItem("Tools/Custom/Child Giver")]
    public static void ShowWindow()
    {
        GetWindow<ChildGiver>("ChildGiver");
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        GUILayout.Label("Add Prefab As Child", EditorStyles.boldLabel);

        // Use SerializedObject so the prefab field supports drag-and-drop
        // and respects undo like any normal Inspector field.
        SerializedObject so = new(this);
        SerializedProperty prefabProp = so.FindProperty("prefabToAdd");
        EditorGUILayout.PropertyField(prefabProp);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();

        resetLocalPosition = EditorGUILayout.Toggle("Reset Local Position", resetLocalPosition);
        resetLocalRotation = EditorGUILayout.Toggle("Reset Local Rotation", resetLocalRotation);
        resetLocalScale    = EditorGUILayout.Toggle("Reset Local Scale",    resetLocalScale);

        EditorGUILayout.Space();

        // Disable the button when there is nothing to act on.
        using (new EditorGUI.DisabledScope(prefabToAdd == null || Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Add Prefab To Selected"))
                AddPrefabToSelection();
        }
    }

    #endregion

    #region Core Logic

    /// <summary>
    /// Instantiates <see cref="prefabToAdd"/> under each selected GameObject.
    /// Preserves the prefab connection when the source is a project prefab asset.
    /// All created instances are grouped into one undo step.
    /// </summary>
    private void AddPrefabToSelection()
    {
        if (prefabToAdd == null)
        {
            Debug.LogWarning("No prefab assigned.");
            return;
        }

        GameObject[] selection = Selection.gameObjects;

        // Open a new undo group so all instances collapse into one Ctrl+Z action.
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject parent in selection)
        {
            if (parent == null) continue;

            GameObject instance;

            // InstantiatePrefab keeps the prefab link intact in the scene.
            // Plain Instantiate is used as a fallback for non-asset GameObjects.
            if (PrefabUtility.IsPartOfPrefabAsset(prefabToAdd))
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToAdd);
            else
                instance = Instantiate(prefabToAdd);

            if (instance == null)
            {
                Debug.LogError($"Failed to instantiate prefab: {prefabToAdd.name}");
                continue;
            }

            // Register before parenting so the full operation is undoable.
            Undo.RegisterCreatedObjectUndo(instance, "Add Prefab Child");

            // worldPositionStays = false keeps the local transform values
            // relative to the new parent rather than trying to preserve world position.
            instance.transform.SetParent(parent.transform, false);

            if (resetLocalPosition) instance.transform.localPosition = Vector3.zero;
            if (resetLocalRotation) instance.transform.localRotation = Quaternion.identity;
            if (resetLocalScale)    instance.transform.localScale    = Vector3.one;

            instance.name = prefabToAdd.name;
        }

        // Merge all individual undo records into one entry in the undo history.
        Undo.CollapseUndoOperations(undoGroup);
    }

    #endregion
}