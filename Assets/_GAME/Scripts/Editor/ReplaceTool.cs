using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;

public class ReplaceTool : EditorWindow
{
    private string searchString = "";
    private GameObject replacementPrefab;

    // Menu Item
    [MenuItem("Tools/Custom/ObjectReplacer")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceTool>("Objet Replacer");
    }

    // UI
    private void OnGUI()
    {
        GUILayout.Label("Object Replacer", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // String Input
        GUILayout.Label("Search Criteria", EditorStyles.miniLabel);
        searchString = EditorGUILayout.TextField("Name Contains:", searchString);
        EditorGUILayout.Space(10);

        // Prefab Input
        GUILayout.Label("Replacement Prefab", EditorStyles.miniLabel);
        replacementPrefab = (GameObject)EditorGUILayout.ObjectField("Replacement Prefab:", replacementPrefab, typeof(GameObject), false);
        EditorGUILayout.Space(20);

        // Confirmation
        GUI.enabled = !string.IsNullOrEmpty(searchString) && replacementPrefab != null;
        if (GUILayout.Button("Find and Replace Object"))
            ReplaceObjects();
        GUI.enabled = true;

        if (Event.current.type == EventType.Repaint)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;
        }
        EditorGUILayout.Space(10);
    }

    // Logic

    private void ReplaceObjects()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<GameObject> objectsToReplace = new();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains(searchString))
                objectsToReplace.Add(obj);
        }

        if (objectsToReplace.Count == 0)
        {
            EditorUtility.DisplayDialog("Replacement Tool", $"No GameObjects found with names containing '{searchString}'.", "OK");
            return;
        }

        // Confirmation dialog
        bool confirmed = EditorUtility.DisplayDialog(
            "Confirm Replacement",
            $"Are you sure you want to replace {objectsToReplace.Count} objects containing '{searchString}' with '{replacementPrefab.name}'?",
            "Yes, Replace Them",
            "Cancel"
        );

        if (!confirmed) return;

        int replacedCount = 0;

        // Start a group for the undo operation
        Undo.SetCurrentGroupName("Replace Objects by String Name");
        int undoGroupIndex = Undo.GetCurrentGroup();

        // Perform the replacement
        foreach (GameObject oldObject in objectsToReplace)
        {
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(replacementPrefab);

            if (newObject != null)
            {
                // Store original position
                Vector3 oldPosition = oldObject.transform.localPosition;
                Vector3 oldScale = oldObject.transform.localScale;
                Quaternion oldRotation = oldObject.transform.localRotation;
                Transform oldParent = oldObject.transform.parent;
                string oldName = oldObject.name;

                // Record the old object for destruction (allows undo to bring it back)
                Undo.DestroyObjectImmediate(oldObject);

                // Set parent for the new object
                if (oldParent != null)
                {
                    Undo.SetTransformParent(newObject.transform, oldParent, "Set Parent");
                }

                // Set position, rotation, and scale
                newObject.transform.localPosition = oldPosition;
                newObject.transform.localRotation = oldRotation;
                newObject.transform.localScale = oldScale;


                newObject.name = oldName.Replace(searchString, replacementPrefab.name);
                replacedCount++;
            }
        }
        // Create a single undo action
        Undo.CollapseUndoOperations(undoGroupIndex);

        // Mark the current scene as dirty to ensure changes are saved
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Replacement Complete", $"Successfully replaced {replacedCount} objects.", "OK");
    }
}
