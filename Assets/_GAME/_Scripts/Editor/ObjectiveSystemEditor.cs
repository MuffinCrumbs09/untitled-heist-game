using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectiveSystem))]
public class ObjectiveSystemEditor : Editor
{
    private SerializedProperty objectiveListProperty;
    private bool[] objectiveFoldouts;
    private bool[][] taskFoldouts;

    private void OnEnable()
    {
        objectiveListProperty = serializedObject.FindProperty("ObjectiveList");
        InitializeFoldouts();
    }

    private void InitializeFoldouts()
    {
        int objectiveCount = objectiveListProperty.arraySize;

        bool[] oldObjectiveFoldouts = objectiveFoldouts;
        bool[][] oldTaskFoldouts = taskFoldouts;

        objectiveFoldouts = new bool[objectiveCount];
        taskFoldouts = new bool[objectiveCount][];

        for (int i = 0; i < objectiveCount; i++)
        {
            if (oldObjectiveFoldouts != null && i < oldObjectiveFoldouts.Length)
                objectiveFoldouts[i] = oldObjectiveFoldouts[i];

            var objectiveProperty = objectiveListProperty.GetArrayElementAtIndex(i);
            var tasksProperty = objectiveProperty.FindPropertyRelative("tasks");
            int taskCount = tasksProperty.arraySize;

            taskFoldouts[i] = new bool[taskCount];

            if (oldTaskFoldouts != null && i < oldTaskFoldouts.Length && oldTaskFoldouts[i] != null)
            {
                for (int j = 0; j < taskCount; j++)
                {
                    if (j < oldTaskFoldouts[i].Length)
                        taskFoldouts[i][j] = oldTaskFoldouts[i][j];
                    else
                        taskFoldouts[i][j] = true;
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Objective System  (Server-Authoritative)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "CurrentObjectiveIndex is a NetworkVariable — do not edit it directly in Play mode. " +
            "Task completion is replicated via NetworkList<bool>.",
            MessageType.Info
        );
        EditorGUILayout.Space(5);

        if (objectiveFoldouts.Length != objectiveListProperty.arraySize)
            InitializeFoldouts();

        for (int i = 0; i < objectiveListProperty.arraySize; i++)
        {
            DrawObjective(i);
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Add New Objective", GUILayout.Height(30)))
            AddNewObjective();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawObjective(int objectiveIndex)
    {
        var objectiveProperty    = objectiveListProperty.GetArrayElementAtIndex(objectiveIndex);
        var objectiveNameProp    = objectiveProperty.FindPropertyRelative("objectiveName");
        var speakerNameProp      = objectiveProperty.FindPropertyRelative("speakerName");
        var speechProp           = objectiveProperty.FindPropertyRelative("speech");
        var tasksProp            = objectiveProperty.FindPropertyRelative("tasks");

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal();
        objectiveFoldouts[objectiveIndex] = EditorGUILayout.Foldout(
            objectiveFoldouts[objectiveIndex],
            string.IsNullOrEmpty(objectiveNameProp.stringValue) ? "Unnamed Objective" : objectiveNameProp.stringValue,
            true
        );

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            objectiveListProperty.DeleteArrayElementAtIndex(objectiveIndex);
            InitializeFoldouts();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (objectiveFoldouts[objectiveIndex])
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(objectiveNameProp, new GUIContent("Objective Name"));
            EditorGUILayout.PropertyField(speakerNameProp,   new GUIContent("Speaker's Name"));
            EditorGUILayout.PropertyField(speechProp,        new GUIContent("Speech"));
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Tasks:", EditorStyles.boldLabel);

            if (taskFoldouts[objectiveIndex].Length != tasksProp.arraySize)
            {
                bool[] old = taskFoldouts[objectiveIndex];
                taskFoldouts[objectiveIndex] = new bool[tasksProp.arraySize];
                for (int j = 0; j < tasksProp.arraySize; j++)
                    taskFoldouts[objectiveIndex][j] = j < old.Length ? old[j] : true;
            }

            for (int j = 0; j < tasksProp.arraySize; j++)
                DrawTask(tasksProp, j, objectiveIndex);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Add Task");

            string[] taskOptions = { "Select Task Type", "Minigame Task", "Timer Task", "Location Task", "Loot Task", "Custom Task" };
            int selectedIndex = EditorGUILayout.Popup(0, taskOptions);

            if (selectedIndex > 0)
            {
                System.Type taskType = selectedIndex switch
                {
                    1 => typeof(MinigameTask),
                    2 => typeof(TimerTask),
                    3 => typeof(LocationTask),
                    4 => typeof(LootTask),
                    5 => typeof(CustomTask),
                    _ => null
                };
                if (taskType != null) AddTask(tasksProp, taskType, objectiveIndex);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawTask(SerializedProperty tasksProp, int taskIndex, int objectiveIndex)
    {
        var taskProperty = tasksProp.GetArrayElementAtIndex(taskIndex);

        if (taskProperty.managedReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Task is null. Please remove and add a new task.", MessageType.Warning);
            if (GUILayout.Button("Remove Null Task"))
                tasksProp.DeleteArrayElementAtIndex(taskIndex);
            return;
        }

        Task task = taskProperty.managedReferenceValue as Task;
        string typeName    = task.GetType().Name;
        string displayName = string.IsNullOrEmpty(task.taskName) ? $"Unnamed {typeName}" : task.taskName;

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.BeginHorizontal();
        taskFoldouts[objectiveIndex][taskIndex] = EditorGUILayout.Foldout(
            taskFoldouts[objectiveIndex][taskIndex],
            $"{displayName} ({typeName})",
            true
        );

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            tasksProp.DeleteArrayElementAtIndex(taskIndex);
            bool[] old = taskFoldouts[objectiveIndex];
            taskFoldouts[objectiveIndex] = new bool[tasksProp.arraySize];
            for (int i = 0; i < tasksProp.arraySize; i++)
            {
                int oldI = i >= taskIndex ? i + 1 : i;
                if (oldI < old.Length) taskFoldouts[objectiveIndex][i] = old[oldI];
            }
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (taskFoldouts[objectiveIndex][taskIndex])
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(taskProperty, new GUIContent("Task Settings"), true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void AddNewObjective()
    {
        objectiveListProperty.arraySize++;
        var newEl = objectiveListProperty.GetArrayElementAtIndex(objectiveListProperty.arraySize - 1);
        newEl.FindPropertyRelative("objectiveName").stringValue = "New Objective";
        newEl.FindPropertyRelative("tasks").ClearArray();
        serializedObject.ApplyModifiedProperties();

        bool[] old = objectiveFoldouts;
        objectiveFoldouts = new bool[objectiveListProperty.arraySize];
        for (int i = 0; i < old.Length; i++) objectiveFoldouts[i] = old[i];
        objectiveFoldouts[objectiveFoldouts.Length - 1] = true;
        InitializeFoldouts();
    }

    private void AddTask(SerializedProperty tasksProp, System.Type taskType, int objectiveIndex)
    {
        int oldSize = tasksProp.arraySize;
        tasksProp.arraySize++;
        var newEl = tasksProp.GetArrayElementAtIndex(tasksProp.arraySize - 1);

        newEl.managedReferenceValue = taskType switch
        {
            var t when t == typeof(MinigameTask) => new MinigameTask { taskName = "New Minigame Task" },
            var t when t == typeof(TimerTask)    => new TimerTask    { taskName = "New Timer Task", timerDuration = 10f },
            var t when t == typeof(LocationTask) => new LocationTask { taskName = "New Location Task" },
            var t when t == typeof(LootTask)     => new LootTask     { taskName = "New Loot Task" },
            var t when t == typeof(CustomTask)   => new CustomTask   { taskName = "New Custom Task" },
            _ => null
        };

        serializedObject.ApplyModifiedProperties();

        bool[] old = taskFoldouts[objectiveIndex];
        taskFoldouts[objectiveIndex] = new bool[tasksProp.arraySize];
        for (int i = 0; i < oldSize; i++) taskFoldouts[objectiveIndex][i] = old[i];
        taskFoldouts[objectiveIndex][oldSize] = true;
    }
}