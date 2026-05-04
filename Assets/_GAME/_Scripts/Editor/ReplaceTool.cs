using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// ─────────────────────────────────────────────────────────────────────────────
//  ReplaceTool.cs
//
//  Editor window that swaps selected scene objects for a chosen replacement
//  prefab, preserving each object's transform by default.
//
//  Open via: Tools > Custom > ObjectReplacer
//
//  Usage:
//    1. Select one or more GameObjects in the Hierarchy.
//    2. Assign the desired replacement prefab.
//    3. Optionally tick "Reset Local ..." to zero out transform values.
//    4. Click "Replace Selected Objects" and confirm.
//
//  Each replacement keeps the original's local position/rotation/scale unless
//  a reset toggle is enabled. The entire operation is one Ctrl+Z undo step.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Editor window that replaces each selected GameObject with an instantiated
/// copy of a replacement prefab, with optional local transform resets and
/// full undo support.
/// </summary>
public class ReplaceTool : EditorWindow
{
    #region Private State

    // The prefab to instantiate in place of each selected object.
    private GameObject replacementPrefab;

    // When true, the corresponding transform component is zeroed instead of
    // inheriting the value from the original object.
    private bool resetLocalPosition = false;
    private bool resetLocalRotation = false;
    private bool resetLocalScale    = false;

    #endregion

    #region Menu Item

    /// <summary>Opens the Object Replacer window from the Unity menu bar.</summary>
    [MenuItem("Tools/Custom/ObjectReplacer")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceTool>("Object Replacer");
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        GUILayout.Label("Replace Selected Objects", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        replacementPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Replacement Prefab",
            replacementPrefab,
            typeof(GameObject),
            false // Project assets only — no scene objects.
        );

        EditorGUILayout.Space(10);

        resetLocalPosition = EditorGUILayout.Toggle("Reset Local Position", resetLocalPosition);
        resetLocalRotation = EditorGUILayout.Toggle("Reset Local Rotation", resetLocalRotation);
        resetLocalScale    = EditorGUILayout.Toggle("Reset Local Scale",    resetLocalScale);

        EditorGUILayout.Space(20);

        // Disable button when there is no prefab or nothing selected.
        GUI.enabled = replacementPrefab != null && Selection.gameObjects.Length > 0;

        if (GUILayout.Button("Replace Selected Objects"))
            ReplaceSelectedObjects();

        GUI.enabled = true;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(
            $"Selected Objects: {Selection.gameObjects.Length}",
            EditorStyles.miniLabel
        );
    }

    #endregion

    #region Core Logic

    /// <summary>
    /// For each selected GameObject, instantiates the replacement prefab at
    /// the same sibling position in the hierarchy, copies the original's
    /// transform, applies any reset overrides, then destroys the original.
    /// All actions are grouped into one undo step.
    /// </summary>
    private void ReplaceSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Replace Tool", "No GameObjects selected.", "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Confirm Replacement",
            $"Replace {selectedObjects.Length} selected object(s) with '{replacementPrefab.name}'?",
            "Yes, Replace",
            "Cancel"
        );

        if (!confirmed) return;

        Undo.SetCurrentGroupName("Replace Selected Objects");
        int undoGroup = Undo.GetCurrentGroup();

        int replacedCount = 0;

        foreach (GameObject oldObject in selectedObjects)
        {
            Transform oldTransform = oldObject.transform;

            // Instantiate under the same parent so the new object sits in the
            // same place in the Hierarchy as the one it replaces.
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(
                replacementPrefab,
                oldTransform.parent
            );

            if (newObject == null) continue;

            Undo.RegisterCreatedObjectUndo(newObject, "Create Replacement");

            // Copy the original's transform first so the replacement lands in
            // exactly the same position/orientation by default.
            newObject.transform.localPosition = oldTransform.localPosition;
            newObject.transform.localRotation = oldTransform.localRotation;
            newObject.transform.localScale    = oldTransform.localScale;

            // Apply reset overrides after copying, so they always win.
            if (resetLocalPosition) newObject.transform.localPosition = Vector3.zero;
            if (resetLocalRotation) newObject.transform.localRotation = Quaternion.identity;
            if (resetLocalScale)    newObject.transform.localScale    = Vector3.one;

            // DestroyObjectImmediate is registered with Undo so it can be undone.
            Undo.DestroyObjectImmediate(oldObject);
            replacedCount++;
        }

        // Merge all create/destroy records into one Ctrl+Z step.
        Undo.CollapseUndoOperations(undoGroup);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Replacement Complete",
            $"Successfully replaced {replacedCount} objects.",
            "OK"
        );
    }

    #endregion
}