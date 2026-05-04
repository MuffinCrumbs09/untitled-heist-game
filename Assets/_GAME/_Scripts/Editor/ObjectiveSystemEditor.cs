using UnityEditor;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  ObjectiveSystemEditor.cs
//
//  Custom Inspector for the ObjectiveSystem component.
//
//  Why this exists:
//    ObjectiveSystem uses [SerializeReference] for its Task list, which means
//    Unity's default Inspector can't show a type-picker dropdown or render
//    polymorphic task types cleanly. This editor adds:
//      - Collapsible foldouts per Objective and per Task.
//      - A dropdown for adding tasks by type (Minigame, Timer, Location, etc.).
//      - Delete buttons on Objectives and Tasks.
//      - An info banner reminding developers about the network constraints.
//
//  Foldout state is stored in plain bool arrays (not EditorPrefs) so it resets
//  on each domain reload, keeping the implementation simple.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replaces the default Inspector for ObjectiveSystem with a structured,
/// foldout-based layout that supports adding and removing polymorphic Tasks.
/// </summary>
[CustomEditor(typeof(ObjectiveSystem))]
public class ObjectiveSystemEditor : Editor
{
    #region Private State

    // Serialized handle to ObjectiveSystem.ObjectiveList.
    private SerializedProperty objectiveListProperty;

    // Tracks which Objective foldouts are open. Indexed by objective position.
    private bool[] objectiveFoldouts;

    // Tracks which Task foldouts are open. First index = objective, second = task.
    private bool[][] taskFoldouts;

    #endregion

    #region Editor Lifecycle

    private void OnEnable()
    {
        objectiveListProperty = serializedObject.FindProperty("ObjectiveList");
        InitializeFoldouts();
    }

    #endregion

    #region Foldout Initialisation

    /// <summary>
    /// Rebuilds the foldout arrays to match the current list sizes.
    /// Preserves existing open/closed state when the array grows or shrinks
    /// (e.g. after an Objective or Task is added/removed).
    /// </summary>
    private void InitializeFoldouts()
    {
        int objectiveCount = objectiveListProperty.arraySize;

        bool[]   oldObjectiveFoldouts = objectiveFoldouts;
        bool[][] oldTaskFoldouts      = taskFoldouts;

        objectiveFoldouts = new bool[objectiveCount];
        taskFoldouts      = new bool[objectiveCount][];

        for (int i = 0; i < objectiveCount; i++)
        {
            // Carry over the previous open/closed state where possible.
            if (oldObjectiveFoldouts != null && i < oldObjectiveFoldouts.Length)
                objectiveFoldouts[i] = oldObjectiveFoldouts[i];

            var objectiveProperty = objectiveListProperty.GetArrayElementAtIndex(i);
            var tasksProperty     = objectiveProperty.FindPropertyRelative("tasks");
            int taskCount         = tasksProperty.arraySize;

            taskFoldouts[i] = new bool[taskCount];

            if (oldTaskFoldouts != null && i < oldTaskFoldouts.Length && oldTaskFoldouts[i] != null)
            {
                for (int j = 0; j < taskCount; j++)
                {
                    // New tasks default to open (true) so the designer sees them immediately.
                    taskFoldouts[i][j] = j < oldTaskFoldouts[i].Length
                        ? oldTaskFoldouts[i][j]
                        : true;
                }
            }
        }
    }

    #endregion

    #region Inspector Root

    /// <summary>
    /// Draws the full custom Inspector. Renders the network info banner,
    /// then one collapsible block per Objective, then an "Add Objective" button.
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Objective System  (Server-Authoritative)", EditorStyles.boldLabel);

        // Remind developers that these values are network-managed so they
        // don't try to hand-edit them during Play mode.
        EditorGUILayout.HelpBox(
            "CurrentObjectiveIndex is a NetworkVariable — do not edit it directly in Play mode. " +
            "Task completion is replicated via NetworkList<bool>.",
            MessageType.Info
        );
        EditorGUILayout.Space(5);

        // Re-sync foldout arrays whenever the list size changes.
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

    #endregion

    #region Objective Drawing

    /// <summary>
    /// Renders one Objective as a collapsible box containing its metadata
    /// fields, its task list, and an "Add Task" type-picker dropdown.
    /// </summary>
    /// <param name="objectiveIndex">Position of this Objective in ObjectiveList.</param>
    private void DrawObjective(int objectiveIndex)
    {
        var objectiveProperty = objectiveListProperty.GetArrayElementAtIndex(objectiveIndex);
        var nameProp          = objectiveProperty.FindPropertyRelative("objectiveName");
        var speakerProp       = objectiveProperty.FindPropertyRelative("speakerName");
        var speechProp        = objectiveProperty.FindPropertyRelative("speech");
        var tasksProp         = objectiveProperty.FindPropertyRelative("tasks");

        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Header row: foldout label + delete button.
        EditorGUILayout.BeginHorizontal();
        objectiveFoldouts[objectiveIndex] = EditorGUILayout.Foldout(
            objectiveFoldouts[objectiveIndex],
            string.IsNullOrEmpty(nameProp.stringValue) ? "Unnamed Objective" : nameProp.stringValue,
            true
        );

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            objectiveListProperty.DeleteArrayElementAtIndex(objectiveIndex);
            InitializeFoldouts();
            return; // Early-out: the array has changed — don't draw stale data.
        }
        EditorGUILayout.EndHorizontal();

        if (objectiveFoldouts[objectiveIndex])
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(nameProp,    new GUIContent("Objective Name"));
            EditorGUILayout.PropertyField(speakerProp, new GUIContent("Speaker's Name"));
            EditorGUILayout.PropertyField(speechProp,  new GUIContent("Speech"));
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Tasks:", EditorStyles.boldLabel);

            // Resize task foldout array if the task count has changed.
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

            // Task type picker — selecting a type immediately adds a new task.
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

                if (taskType != null)
                    AddTask(tasksProp, taskType, objectiveIndex);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Task Drawing

    /// <summary>
    /// Renders one Task as a collapsible box with a type label and delete button.
    /// Shows a warning and a remove button if the managed reference is null
    /// (can happen after script renames or missing assemblies).
    /// </summary>
    /// <param name="tasksProp">The tasks SerializedProperty from the parent Objective.</param>
    /// <param name="taskIndex">Index of this Task within the tasks array.</param>
    /// <param name="objectiveIndex">Index of the parent Objective — needed to update foldout state.</param>
    private void DrawTask(SerializedProperty tasksProp, int taskIndex, int objectiveIndex)
    {
        var taskProperty = tasksProp.GetArrayElementAtIndex(taskIndex);

        // Guard against null managed references left behind by bad data or script changes.
        if (taskProperty.managedReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Task is null. Please remove and add a new task.", MessageType.Warning);
            if (GUILayout.Button("Remove Null Task"))
                tasksProp.DeleteArrayElementAtIndex(taskIndex);
            return;
        }

        Task   task        = taskProperty.managedReferenceValue as Task;
        string typeName    = task.GetType().Name;
        string displayName = string.IsNullOrEmpty(task.taskName)
            ? $"Unnamed {typeName}"
            : task.taskName;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Header row: foldout with type suffix + delete button.
        EditorGUILayout.BeginHorizontal();
        taskFoldouts[objectiveIndex][taskIndex] = EditorGUILayout.Foldout(
            taskFoldouts[objectiveIndex][taskIndex],
            $"{displayName} ({typeName})",
            true
        );

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            tasksProp.DeleteArrayElementAtIndex(taskIndex);

            // Rebuild the task foldout array, skipping the deleted index.
            bool[] old = taskFoldouts[objectiveIndex];
            taskFoldouts[objectiveIndex] = new bool[tasksProp.arraySize];
            for (int i = 0; i < tasksProp.arraySize; i++)
            {
                int oldI = i >= taskIndex ? i + 1 : i;
                if (oldI < old.Length)
                    taskFoldouts[objectiveIndex][i] = old[oldI];
            }
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (taskFoldouts[objectiveIndex][taskIndex])
        {
            EditorGUI.indentLevel++;
            // PropertyField with includeChildren=true draws all subclass fields automatically.
            EditorGUILayout.PropertyField(taskProperty, new GUIContent("Task Settings"), true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Add / Remove Helpers

    /// <summary>
    /// Appends a blank Objective to ObjectiveList and opens its foldout
    /// so the designer can immediately start filling it in.
    /// </summary>
    private void AddNewObjective()
    {
        objectiveListProperty.arraySize++;
        var newEl = objectiveListProperty.GetArrayElementAtIndex(objectiveListProperty.arraySize - 1);
        newEl.FindPropertyRelative("objectiveName").stringValue = "New Objective";
        newEl.FindPropertyRelative("tasks").ClearArray();
        serializedObject.ApplyModifiedProperties();

        // Expand the foldout array and open the new entry.
        bool[] old = objectiveFoldouts;
        objectiveFoldouts = new bool[objectiveListProperty.arraySize];
        for (int i = 0; i < old.Length; i++) objectiveFoldouts[i] = old[i];
        objectiveFoldouts[objectiveFoldouts.Length - 1] = true;

        InitializeFoldouts();
    }

    /// <summary>
    /// Appends a new Task of the given type to <paramref name="tasksProp"/>
    /// with sensible default values, then opens its foldout.
    /// </summary>
    /// <param name="tasksProp">The tasks array of the parent Objective.</param>
    /// <param name="taskType">Concrete Task subclass to instantiate.</param>
    /// <param name="objectiveIndex">Parent Objective index — needed for foldout tracking.</param>
    private void AddTask(SerializedProperty tasksProp, System.Type taskType, int objectiveIndex)
    {
        int oldSize = tasksProp.arraySize;
        tasksProp.arraySize++;
        var newEl = tasksProp.GetArrayElementAtIndex(tasksProp.arraySize - 1);

        // Assign a concrete instance with sensible defaults so the designer
        // sees meaningful starting values rather than zeros.
        newEl.managedReferenceValue = taskType switch
        {
            var t when t == typeof(MinigameTask) => new MinigameTask { taskName = "New Minigame Task" },
            var t when t == typeof(TimerTask)    => new TimerTask    { taskName = "New Timer Task", timerDuration = 10f },
            var t when t == typeof(LocationTask) => new LocationTask { taskName = "New Location Task" },
            var t when t == typeof(LootTask)     => new LootTask     { taskName = "New Loot Task" },
            var t when t == typeof(CustomTask)   => new CustomTask   { taskName = "New Custom Task" },
            _                                    => null
        };

        serializedObject.ApplyModifiedProperties();

        // Expand foldout array and open the newly added task.
        bool[] old = taskFoldouts[objectiveIndex];
        taskFoldouts[objectiveIndex] = new bool[tasksProp.arraySize];
        for (int i = 0; i < oldSize; i++) taskFoldouts[objectiveIndex][i] = old[i];
        taskFoldouts[objectiveIndex][oldSize] = true;
    }

    #endregion
}