using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;

public class RenameTool : EditorWindow
{
    private string searchString = "";
    private string replacementName = "";

    // Menu Item
    [MenuItem("Tools/Custom/RenameTool")]
    public static void ShowWindow()
    {
        GetWindow<RenameTool>("Rename Tool");
    }

    // UI
    private void OnGUI()
    {
        GUILayout.Label("Object Renamer", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // New Name Input
        GUILayout.Label("Replacement Name", EditorStyles.miniLabel);
        replacementName = EditorGUILayout.TextField("Replacement Name:", replacementName);
        EditorGUILayout.Space(20);

        // Confirmation
        GUI.enabled = !string.IsNullOrEmpty(searchString) && !string.IsNullOrEmpty(replacementName);
        if (GUILayout.Button("Find and Rename Objects"))
            RenameObjects();
        GUI.enabled = true;

        if (Event.current.type == EventType.Repaint)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;
        }
        EditorGUILayout.Space(10);
    }

    // Logic

    private void RenameObjects()
    {
        // Get Selected Objects
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Rename Tool",
                "No GameObjects selected.",
                "OK"
            );
            return;
        }
        // Confirmation dialog
        bool confirmed = EditorUtility.DisplayDialog(
            "Confirm Replacement",
            $"Are you sure you want to rename {selectedObjects.Length} objects containing '{searchString}' with '{replacementName}'?",
            "Yes, Rename Them",
            "Cancel"
        );

        if (!confirmed) return;

        int renameCount = 0;

        // Rename
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            GameObject rename = selectedObjects[i];
            rename.name = $"{replacementName} {i}";

            renameCount++;
        }

        // Mark the current scene as dirty to ensure changes are saved
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Rename Complete", $"Successfully renamed {renameCount} objects.", "OK");
    }
}
