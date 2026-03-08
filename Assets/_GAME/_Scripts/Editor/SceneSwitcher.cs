using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SceneSwitcher : EditorWindow
{
    [MenuItem("Tools/Custom/Scene Switcher")]
    public static void ShowWindow()
    {
        GetWindow<SceneSwitcher>("Scene Switcher");
    }

    private void OnGUI()
    {
        GUILayout.Label("Quick Swap Scenes", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Length == 0)
        {
            EditorGUILayout.HelpBox("No scenes found. Make sure they are in build settings!", MessageType.Warning);
            return;
        }

        foreach (var scene in scenes)
        {
            if (scene.enabled)
            {
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);

                if (GUILayout.Button($"Open {sceneName}", GUILayout.Height(30)))
                {
                    SwapScene(scene.path);
                }
            }
        }
    }

    private void SwapScene(string scenePath)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }
}
