using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// ─────────────────────────────────────────────────────────────────────────────
//  RenameTool.cs
//
//  Editor window that batch-renames all selected GameObjects in the scene.
//
//  Open via: Tools > Custom > RenameTool
//
//  Usage:
//    1. Select one or more GameObjects in the Hierarchy.
//    2. Type the new base name in the "Replacement Name" field.
//    3. Click "Find and Rename Objects".
//    4. Confirm the dialog — each selected object is renamed to
//       "<ReplacementName> 0", "<ReplacementName> 1", etc.
//
//  The scene is marked dirty after renaming so changes aren't lost on close.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Editor window that renames every selected GameObject in the active scene
/// to a numbered sequence based on a user-supplied base name.
/// </summary>
public class RenameTool : EditorWindow
{
    #region Private State

    // Not currently used for filtering — reserved for a future find-and-replace mode.
    private string searchString = "";

    // The base name applied to all selected objects, suffixed with an index.
    private string replacementName = "";

    #endregion

    #region Menu Item

    /// <summary>Opens the Rename Tool window from the Unity menu bar.</summary>
    [MenuItem("Tools/Custom/RenameTool")]
    public static void ShowWindow()
    {
        GetWindow<RenameTool>("Rename Tool");
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        GUILayout.Label("Object Renamer", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        GUILayout.Label("Replacement Name", EditorStyles.miniLabel);
        replacementName = EditorGUILayout.TextField("Replacement Name:", replacementName);
        EditorGUILayout.Space(20);

        // Only enable the rename button when a name has been entered.
        GUI.enabled = !string.IsNullOrEmpty(replacementName);

        if (GUILayout.Button("Find and Rename Objects"))
            RenameObjects();

        GUI.enabled = true;

        EditorGUILayout.Space(10);
    }

    #endregion

    #region Core Logic

    /// <summary>
    /// Renames each selected GameObject to "<replacementName> N" where N is
    /// the object's position in the selection array. Shows a confirmation
    /// dialog before making any changes and marks the scene dirty afterwards.
    /// </summary>
    private void RenameObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Rename Tool", "No GameObjects selected.", "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Confirm Replacement",
            $"Are you sure you want to rename {selectedObjects.Length} objects to '{replacementName} [0..{selectedObjects.Length - 1}]'?",
            "Yes, Rename Them",
            "Cancel"
        );

        if (!confirmed) return;

        int renameCount = 0;

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            selectedObjects[i].name = $"{replacementName} {i}";
            renameCount++;
        }

        // Mark dirty so Unity knows the scene has unsaved changes.
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Rename Complete",
            $"Successfully renamed {renameCount} objects.",
            "OK"
        );
    }

    #endregion
}