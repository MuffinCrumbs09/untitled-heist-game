using UnityEngine;
using UnityEditor;

public class ChildGiver : EditorWindow
{
    [SerializeField] private GameObject prefabToAdd;

    private bool resetLocalPosition = true;
    private bool resetLocalRotation = true;
    private bool resetLocalScale = true;

    [MenuItem("Tools/Custom/Child Giver")]
    public static void ShowWindow()
    {
        GetWindow<ChildGiver>("ChildGiver");
    }

    private void OnGUI()
    {
        GUILayout.Label("Add Prefab As Child", EditorStyles.boldLabel);

        SerializedObject so = new(this);
        SerializedProperty prefabProp = so.FindProperty("prefabToAdd");
        EditorGUILayout.PropertyField(prefabProp);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();

        resetLocalPosition = EditorGUILayout.Toggle("Reset Local Position", resetLocalPosition);
        resetLocalRotation = EditorGUILayout.Toggle("Reset Local Rotation", resetLocalRotation);
        resetLocalScale = EditorGUILayout.Toggle("Reset Local Scale", resetLocalScale);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(prefabToAdd == null || Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Add Prefab To Selected"))
            {
                AddPrefabToSelection();
            }
        }
    }

    private void AddPrefabToSelection()
    {
        if (prefabToAdd == null)
        {
            Debug.LogWarning("No prefab assigned.");
            return;
        }

        GameObject[] selection = Selection.gameObjects;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject parent in selection)
        {
            if (parent == null) continue;

            GameObject instance;

            if (PrefabUtility.IsPartOfPrefabAsset(prefabToAdd))
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToAdd);
            }
            else
            {
                instance = Instantiate(prefabToAdd);
            }

            if (instance == null)
            {
                Debug.LogError($"Failed to instantiate prefab: {prefabToAdd.name}");
                continue;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Add Prefab Child");

            instance.transform.SetParent(parent.transform, false);

            if (resetLocalPosition)
                instance.transform.localPosition = Vector3.zero;

            if (resetLocalRotation)
                instance.transform.localRotation = Quaternion.identity;

            if (resetLocalScale)
                instance.transform.localScale = Vector3.one;

            instance.name = prefabToAdd.name;
        }

        Undo.CollapseUndoOperations(undoGroup);
    }
}