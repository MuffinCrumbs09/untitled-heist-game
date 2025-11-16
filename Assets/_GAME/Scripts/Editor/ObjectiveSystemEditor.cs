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
            {
                objectiveFoldouts[i] = oldObjectiveFoldouts[i];
            }
            else
            {
                objectiveFoldouts[i] = false;
            }

            var objectiveProperty = objectiveListProperty.GetArrayElementAtIndex(i);
            var tasksProperty = objectiveProperty.FindPropertyRelative("tasks");
            int taskCount = tasksProperty.arraySize;

            taskFoldouts[i] = new bool[taskCount];

            if (oldTaskFoldouts != null && i < oldTaskFoldouts.Length && oldTaskFoldouts[i] != null)
            {
                for (int j = 0; j < taskCount; j++)
                {
                    if (j < oldTaskFoldouts[i].Length)
                    {
                        taskFoldouts[i][j] = oldTaskFoldouts[i][j];
                    }
                    else
                    {
                        taskFoldouts[i][j] = true;
                    }
                }
            }
            else
            {
                for (int j = 0; j < taskCount; j++)
                {
                    taskFoldouts[i][j] = false;
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Objective System", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (objectiveFoldouts.Length != objectiveListProperty.arraySize)
        {
            InitializeFoldouts();
        }

        for (int i = 0; i < objectiveListProperty.arraySize; i++)
        {
            DrawObjective(i);
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Add New Objective", GUILayout.Height(30)))
        {
            AddNewObjective();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawObjective(int objectiveIndex)
    {
        var objectiveProperty = objectiveListProperty.GetArrayElementAtIndex(objectiveIndex);
        var objectiveNameProperty = objectiveProperty.FindPropertyRelative("objectiveName");
        var tasksProperty = objectiveProperty.FindPropertyRelative("tasks");

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal();
        objectiveFoldouts[objectiveIndex] = EditorGUILayout.Foldout(
            objectiveFoldouts[objectiveIndex],
            string.IsNullOrEmpty(objectiveNameProperty.stringValue) ? "Unnamed Objective" : objectiveNameProperty.stringValue,
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

            EditorGUILayout.PropertyField(objectiveNameProperty, new GUIContent("Objective Name"));
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Tasks:", EditorStyles.boldLabel);

            if (taskFoldouts[objectiveIndex].Length != tasksProperty.arraySize)
            {
                bool[] oldTaskStates = taskFoldouts[objectiveIndex];
                taskFoldouts[objectiveIndex] = new bool[tasksProperty.arraySize];

                for (int j = 0; j < tasksProperty.arraySize; j++)
                {
                    if (j < oldTaskStates.Length)
                    {
                        taskFoldouts[objectiveIndex][j] = oldTaskStates[j];
                    }
                    else
                    {
                        taskFoldouts[objectiveIndex][j] = true;
                    }
                }
            }

            for (int j = 0; j < tasksProperty.arraySize; j++)
            {
                DrawTask(tasksProperty, j, objectiveIndex);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel("Add Task");
            
            string[] taskOptions = new string[] 
            { 
                "Select Task Type",
                "Minigame Task", 
                "Timer Task", 
                "Location Task", 
                "Loot Task", 
                "Custom Task" 
            };

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

                if (taskType != null)
                {
                    AddTask(tasksProperty, taskType, objectiveIndex);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawTask(SerializedProperty tasksProperty, int taskIndex, int objectiveIndex)
    {
        var taskProperty = tasksProperty.GetArrayElementAtIndex(taskIndex);

        if (taskProperty.managedReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Task is null. Please remove and add a new task.", MessageType.Warning);
            if (GUILayout.Button("Remove Null Task"))
            {
                tasksProperty.DeleteArrayElementAtIndex(taskIndex);
            }
            return;
        }

        Task task = taskProperty.managedReferenceValue as Task;
        string taskTypeName = task.GetType().Name;
        string taskDisplayName = string.IsNullOrEmpty(task.taskName) ? $"Unnamed {taskTypeName}" : task.taskName;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal();
        taskFoldouts[objectiveIndex][taskIndex] = EditorGUILayout.Foldout(
            taskFoldouts[objectiveIndex][taskIndex],
            $"{taskDisplayName} ({taskTypeName})",
            true
        );

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            tasksProperty.DeleteArrayElementAtIndex(taskIndex);
            bool[] oldStates = taskFoldouts[objectiveIndex];
            taskFoldouts[objectiveIndex] = new bool[tasksProperty.arraySize];

            for (int i = 0; i < tasksProperty.arraySize; i++)
            {
                int oldIndex = i >= taskIndex ? i + 1 : i;
                if (oldIndex < oldStates.Length)
                {
                    taskFoldouts[objectiveIndex][i] = oldStates[oldIndex];
                }
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
        var newElement = objectiveListProperty.GetArrayElementAtIndex(objectiveListProperty.arraySize - 1);
        newElement.FindPropertyRelative("objectiveName").stringValue = "New Objective";

        var tasksProperty = newElement.FindPropertyRelative("tasks");
        tasksProperty.ClearArray();

        serializedObject.ApplyModifiedProperties();

        bool[] oldFoldouts = objectiveFoldouts;
        objectiveFoldouts = new bool[objectiveListProperty.arraySize];

        for (int i = 0; i < oldFoldouts.Length; i++)
        {
            objectiveFoldouts[i] = oldFoldouts[i];
        }
        objectiveFoldouts[objectiveFoldouts.Length - 1] = true;

        InitializeFoldouts();
    }

    private void AddTask(SerializedProperty tasksProperty, System.Type taskType, int objectiveIndex)
    {
        int oldSize = tasksProperty.arraySize;
        tasksProperty.arraySize++;
        var newTaskElement = tasksProperty.GetArrayElementAtIndex(tasksProperty.arraySize - 1);

        switch (taskType)
        {
            case var type when type == typeof(MinigameTask):
                newTaskElement.managedReferenceValue = new MinigameTask
                {
                    taskName = "New Minigame Task",
                    isCompleted = false
                };
                break;

            case var type when type == typeof(TimerTask):
                newTaskElement.managedReferenceValue = new TimerTask
                {
                    taskName = "New Timer Task",
                    isCompleted = false,
                    timerDuration = 10f
                };
                break;

            case var type when type == typeof(LocationTask):
                newTaskElement.managedReferenceValue = new LocationTask
                {
                    taskName = "New Location Task",
                    isCompleted = false
                };
                break;

            case var type when type == typeof(LootTask):
                newTaskElement.managedReferenceValue = new LootTask
                {
                    taskName = "New Loot Task",
                    isCompleted = false
                };
                break;

            case var type when type == typeof(CustomTask):
                newTaskElement.managedReferenceValue = new CustomTask
                {
                    taskName = "New Custom Task",
                    isCompleted = false
                };
                break;
        }

        serializedObject.ApplyModifiedProperties();

        bool[] oldTaskStates = taskFoldouts[objectiveIndex];
        taskFoldouts[objectiveIndex] = new bool[tasksProperty.arraySize];

        for (int i = 0; i < oldSize; i++)
        {
            taskFoldouts[objectiveIndex][i] = oldTaskStates[i];
        }
        taskFoldouts[objectiveIndex][oldSize] = true;
    }
}
