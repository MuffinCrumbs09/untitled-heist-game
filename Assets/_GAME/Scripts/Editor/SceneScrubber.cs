using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SceneScrubber : EditorWindow
{
    private string targetRoot = "Production_Assets";

    [MenuItem("Tools/Custom/Scene Scrubber")]
    public static void ShowWindow()
    {
        GetWindow<SceneScrubber>("Scene Scrubber");
    }

    private void OnGUI()
    {
        GUILayout.Label("Organize Scene Assets by Pack", EditorStyles.boldLabel);
        targetRoot = EditorGUILayout.TextField("Export Folder Name", targetRoot);

        if (GUILayout.Button("Pack Active Scene Assets"))
        {
            PackDependencies();
        }
    }

    private void PackDependencies()
    {
        string[] scenePaths = { UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path };
        // Collect everything used in the scene
        string[] dependencies = AssetDatabase.GetDependencies(scenePaths, true);

        string rootPath = "Assets/" + targetRoot;
        if (!AssetDatabase.IsValidFolder(rootPath)) AssetDatabase.CreateFolder("Assets", targetRoot);

        int movedCount = 0;

        foreach (string path in dependencies)
        {
            // Ignore scripts, the scene itself, and built-in Unity engine assets
            if (path.EndsWith(".cs") || path.EndsWith(".unity") || !path.StartsWith("Assets")) continue;
            if (path.StartsWith(rootPath)) continue;

            // Get the "Pack Name" (The first folder after 'Assets/')
            string[] pathParts = path.Split('/');
            string packName = (pathParts.Length > 1) ? pathParts[1] : "Miscellaneous";
            
            // Create the pack sub-folder if it doesn't exist
            string packFolderPath = rootPath + "/" + packName;
            if (!AssetDatabase.IsValidFolder(packFolderPath))
            {
                AssetDatabase.CreateFolder(rootPath, packName);
            }

            // Move the asset
            string fileName = Path.GetFileName(path);
            string destPath = packFolderPath + "/" + fileName;
            
            string error = AssetDatabase.MoveAsset(path, destPath);

            if (string.IsNullOrEmpty(error)) movedCount++;
            else Debug.LogWarning($"Skipped {fileName}: {error}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"Successfully organized {movedCount} assets into {rootPath} grouped by pack.");
    }
}