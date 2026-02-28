using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SceneScrubber : EditorWindow
{
    private string targetRoot = "Production_Assets";

    private List<string> ignoredFolders = new List<string>() {"_GAME", "_ASSETS"};
    private string newIgnoreEntry = "";

    [MenuItem("Tools/Custom/Scene Scrubber")]
    public static void ShowWindow()
    {
        GetWindow<SceneScrubber>("Scene Scrubber");
    }

    private void OnGUI()
    {
        GUILayout.Label("Organize Scene Assets by Pack", EditorStyles.boldLabel);
        targetRoot = EditorGUILayout.TextField("Export Folder Name", targetRoot);

        GUILayout.Space(10);
        GUILayout.Label("Ignored Root Folders (case-insensitive)", EditorStyles.boldLabel);

        for (int i = 0; i < ignoredFolders.Count; i++)
        {
            GUILayout.BeginHorizontal();
            ignoredFolders[i] = EditorGUILayout.TextField(ignoredFolders[i]);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                ignoredFolders.RemoveAt(i);
                i--;
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        newIgnoreEntry = EditorGUILayout.TextField(newIgnoreEntry);
        if (GUILayout.Button("Add", GUILayout.Width(60)))
        {
            if (!string.IsNullOrWhiteSpace(newIgnoreEntry))
            {
                ignoredFolders.Add(newIgnoreEntry.Trim());
                newIgnoreEntry = "";
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (GUILayout.Button("Pack Active Scene Assets"))
        {
            PackDependencies();
        }
    }

    private void PackDependencies()
    {
        string[] scenePaths = { UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path };
        string[] dependencies = AssetDatabase.GetDependencies(scenePaths, true);

        string rootPath = "Assets/" + targetRoot;

        if (!AssetDatabase.IsValidFolder(rootPath))
        {
            AssetDatabase.CreateFolder("Assets", targetRoot);
        }

        int movedCount = 0;

        foreach (string path in dependencies)
        {
            // Normalize path comparison (case-insensitive)
            string normalizedPath = path.ToLowerInvariant();
            string normalizedRoot = rootPath.ToLowerInvariant();

            // Ignore scripts, scenes, non-project assets
            if (normalizedPath.EndsWith(".cs") ||
                normalizedPath.EndsWith(".unity") ||
                !normalizedPath.StartsWith("assets"))
                continue;

            // Ignore if already inside export folder
            if (normalizedPath.StartsWith(normalizedRoot))
                continue;

            string[] pathParts = path.Split('/');

            if (pathParts.Length < 2)
            {
                Debug.LogWarning($"Skipping invalid path: {path}");
                continue;
            }

            string packName = pathParts[1];

            // Check ignored folders (case-insensitive)
            if (ignoredFolders.Any(f =>
                f.Equals(packName, System.StringComparison.OrdinalIgnoreCase)))
            {
                Debug.Log($"Ignored folder match: {packName}");
                continue;
            }

            string packFolderPath = rootPath + "/" + packName;

            // Create pack subfolder safely
            if (!AssetDatabase.IsValidFolder(packFolderPath))
            {
                try
                {
                    AssetDatabase.CreateFolder(rootPath, packName);
                }
                catch
                {
                    Debug.LogWarning($"Could not create folder: {packFolderPath}. Skipping assets inside it.");
                    continue;
                }
            }

            string fileName = Path.GetFileName(path);
            string destPath = packFolderPath + "/" + fileName;

            string error = AssetDatabase.MoveAsset(path, destPath);

            if (string.IsNullOrEmpty(error))
            {
                movedCount++;
            }
            else
            {
                Debug.LogWarning($"Skipped {fileName}: {error}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Successfully organized {movedCount} assets into {rootPath} grouped by pack.");
    }
}