using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ReplaceTool : EditorWindow
{
    private GameObject replacementPrefab;

    [MenuItem("Tools/Custom/ObjectReplacer")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceTool>("Object Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace Selected Objects", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        replacementPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Replacement Prefab",
            replacementPrefab,
            typeof(GameObject),
            false
        );

        EditorGUILayout.Space(20);

        GUI.enabled = replacementPrefab != null && Selection.gameObjects.Length > 0;
        if (GUILayout.Button("Replace Selected Objects"))
        {
            ReplaceSelectedObjects();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(
            $"Selected Objects: {Selection.gameObjects.Length}",
            EditorStyles.miniLabel
        );
    }

    private void ReplaceSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Replace Tool",
                "No GameObjects selected.",
                "OK"
            );
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Confirm Replacement",
            $"Are you sure you want to replace {selectedObjects.Length} selected objects with '{replacementPrefab.name}'?",
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

            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(
                replacementPrefab,
                oldTransform.parent
            );

            if (newObject == null)
                continue;

            Undo.RegisterCreatedObjectUndo(newObject, "Create Replacement");

            // Copy transform data
            newObject.transform.localPosition = oldTransform.localPosition;
            newObject.transform.localRotation = oldTransform.localRotation;
            newObject.transform.localScale = oldTransform.localScale;

            Undo.DestroyObjectImmediate(oldObject);
            replacedCount++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Replacement Complete",
            $"Successfully replaced {replacedCount} objects.",
            "OK"
        );
    }
}
