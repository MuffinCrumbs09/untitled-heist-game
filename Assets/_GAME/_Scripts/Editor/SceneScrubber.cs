using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ─────────────────────────────────────────────────────────────────────────────
//  SceneScrubber.cs
//
//  Editor window that consolidates all assets used by the active scene into a
//  single export folder, grouped by their original root folder (asset pack).
//
//  Open via: Tools > Custom > Scene Scrubber
//
//  How it works:
//    1. Calls AssetDatabase.GetDependencies on the active scene to find every
//       asset it references (meshes, textures, materials, audio, etc.).
//    2. Groups each asset under a sub-folder named after its original root
//       folder (e.g. Assets/SomeAssetPack/Mesh.fbx → Export/SomeAssetPack/).
//    3. Skips scripts, scene files, assets already in the export folder, and
//       any root folders listed in the "Ignored Root Folders" list.
//
//  Useful for shipping a scene to a collaborator without manually hunting
//  down every texture and mesh it depends on.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Editor window that scans the active scene's asset dependencies and moves
/// them into a named export folder, grouped by their source asset pack.
/// </summary>
public class SceneScrubber : EditorWindow
{
    #region Private State

    // Name of the top-level folder created inside Assets/ for the export.
    private string targetRoot = "Production_Assets";

    // Root folders whose contents should never be moved (e.g. your game's own assets).
    // Compared case-insensitively against the second path segment (Assets/<RootFolder>/...).
    private List<string> ignoredFolders = new List<string>() { "_GAME", "_ASSETS" };

    // Staging field for the "Add" text input.
    private string newIgnoreEntry = "";

    #endregion

    #region Menu Item

    /// <summary>Opens the Scene Scrubber window from the Unity menu bar.</summary>
    [MenuItem("Tools/Custom/Scene Scrubber")]
    public static void ShowWindow()
    {
        GetWindow<SceneScrubber>("Scene Scrubber");
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        GUILayout.Label("Organize Scene Assets by Pack", EditorStyles.boldLabel);
        targetRoot = EditorGUILayout.TextField("Export Folder Name", targetRoot);

        GUILayout.Space(10);
        GUILayout.Label("Ignored Root Folders (case-insensitive)", EditorStyles.boldLabel);

        // Draw the current ignore list with per-entry remove buttons.
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

        // Input row for adding a new entry to the ignore list.
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
            PackDependencies();
    }

    #endregion

    #region Core Logic

    /// <summary>
    /// Collects all dependencies of the active scene and moves eligible assets
    /// into the configured export folder, grouped by their original root folder.
    /// </summary>
    private void PackDependencies()
    {
        string[] scenePaths  = { UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path };
        string[] dependencies = AssetDatabase.GetDependencies(scenePaths, true);

        string rootPath = "Assets/" + targetRoot;

        // Create the top-level export folder if it doesn't exist yet.
        if (!AssetDatabase.IsValidFolder(rootPath))
            AssetDatabase.CreateFolder("Assets", targetRoot);

        int movedCount = 0;

        foreach (string path in dependencies)
        {
            string normalizedPath = path.ToLowerInvariant();
            string normalizedRoot = rootPath.ToLowerInvariant();

            // Skip scripts, scene files, and anything outside the Assets folder.
            if (normalizedPath.EndsWith(".cs"))     continue;
            if (normalizedPath.EndsWith(".unity"))  continue;
            if (!normalizedPath.StartsWith("assets")) continue;

            // Skip assets already inside the export folder.
            if (normalizedPath.StartsWith(normalizedRoot)) continue;

            // The second path segment is the root-level folder (the "pack name").
            // e.g. "Assets/FantasyPack/Textures/Stone.png" → packName = "FantasyPack"
            string[] pathParts = path.Split('/');
            if (pathParts.Length < 2)
            {
                Debug.LogWarning($"Skipping invalid path: {path}");
                continue;
            }

            string packName = pathParts[1];

            // Skip folders the designer has flagged as off-limits.
            if (ignoredFolders.Any(f => f.Equals(packName, System.StringComparison.OrdinalIgnoreCase)))
            {
                Debug.Log($"Ignored folder match: {packName}");
                continue;
            }

            string packFolderPath = rootPath + "/" + packName;

            // Create the per-pack sub-folder if needed.
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
                movedCount++;
            else
                Debug.LogWarning($"Skipped {fileName}: {error}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"Successfully organized {movedCount} assets into {rootPath} grouped by pack.");
    }

    #endregion
}