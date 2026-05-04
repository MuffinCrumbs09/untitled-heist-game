using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  SceneSwitcher.cs
//
//  Minimal editor window that lists every enabled scene from Build Settings
//  and lets you switch to any of them with one click.
//
//  Open via: Tools > Custom > Scene Switcher
//
//  Usage:
//    - Click any scene button to open it.
//    - If the current scene has unsaved changes, Unity's standard
//      "Save / Don't Save / Cancel" dialog appears first.
//
//  Only scenes ticked as "enabled" in Build Settings are shown.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Editor window that renders a one-click button for each enabled scene in
/// Build Settings, handling unsaved-changes prompts automatically.
/// </summary>
public class SceneSwitcher : EditorWindow
{
    #region Menu Item

    /// <summary>Opens the Scene Switcher window from the Unity menu bar.</summary>
    [MenuItem("Tools/Custom/Scene Switcher")]
    public static void ShowWindow()
    {
        GetWindow<SceneSwitcher>("Scene Switcher");
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        GUILayout.Label("Quick Swap Scenes", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        if (scenes.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "No scenes found in Build Settings. Add scenes via File > Build Settings.",
                MessageType.Warning
            );
            return;
        }

        // Draw one button per enabled scene. Disabled scenes are hidden to
        // avoid confusion with scenes that won't be built.
        foreach (var scene in scenes)
        {
            if (!scene.enabled) continue;

            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);

            if (GUILayout.Button($"Open {sceneName}", GUILayout.Height(30)))
                SwapScene(scene.path);
        }
    }

    #endregion

    #region Core Logic

    /// <summary>
    /// Prompts the user to save the current scene if it has unsaved changes,
    /// then opens the scene at the given path.
    /// If the user cancels the save dialog, the switch is aborted.
    /// </summary>
    /// <param name="scenePath">Project-relative path to the target scene asset.</param>
    private void SwapScene(string scenePath)
    {
        // SaveCurrentModifiedScenesIfUserWantsTo returns false if the user
        // clicked "Cancel", in which case we should not proceed.
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(scenePath);
    }

    #endregion
}