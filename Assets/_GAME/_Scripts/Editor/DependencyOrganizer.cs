using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
//  DependencyOrganizer.cs
//
//  Asset-menu utility that collects all dependencies of a selected prefab and
//  moves them into a dedicated sub-folder next to that prefab.
//
//  Access via: right-click a prefab in the Project window >
//              "Organize Prefab Dependencies"
//
//  What it does:
//    1. Finds every asset referenced by the chosen prefab (textures, materials,
//       meshes, audio clips, etc.).
//    2. Creates a "<PrefabName>_Dependencies" folder beside the prefab.
//    3. Moves each dependency into that folder.
//
//  What it skips:
//    - The prefab file itself.
//    - MonoScript assets (.cs files) — scripts live in their own folders.
//    - Built-in Unity resources (Library/ and Resources/ prefixes).
//    - Anything not inside the Assets/ folder.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Asset-menu command that gathers a prefab's dependencies and consolidates
/// them into a single folder next to the prefab for easier project organisation.
/// </summary>
public class DependencyOrganizer : EditorWindow
{
    #region Menu Item

    /// <summary>
    /// Entry point. Validates the selection then runs the dependency move.
    /// Must be called with a prefab selected in the Project window.
    /// </summary>
    [MenuItem("Assets/Organize Prefab Dependencies")]
    public static void OrganizeDependencies()
    {
        GameObject selectedPrefab = Selection.activeObject as GameObject;

        if (selectedPrefab == null ||
            PrefabUtility.GetPrefabAssetType(selectedPrefab) == PrefabAssetType.NotAPrefab)
        {
            Debug.LogError("Please select a Prefab in the Project window first.");
            return;
        }

        string prefabPath  = AssetDatabase.GetAssetPath(selectedPrefab);
        string rootPath    = Path.GetDirectoryName(prefabPath);
        string folderName  = selectedPrefab.name + "_Dependencies";
        string targetPath  = Path.Combine(rootPath, folderName);

        // Create the destination folder if it doesn't already exist.
        if (!AssetDatabase.IsValidFolder(targetPath))
            AssetDatabase.CreateFolder(rootPath, folderName);

        // CollectDependencies does a deep scan of every asset the prefab references.
        Object[] dependencies = EditorUtility.CollectDependencies(new Object[] { selectedPrefab });

        int movedCount = 0;

        foreach (Object dep in dependencies)
        {
            string depPath = AssetDatabase.GetAssetPath(dep);

            // Skip the prefab itself, scripts, and non-project resources.
            if (depPath == prefabPath)                  continue;
            if (dep is MonoScript)                      continue;
            if (depPath.StartsWith("Resources/"))       continue;
            if (depPath.StartsWith("Library/"))         continue;
            if (string.IsNullOrEmpty(depPath))          continue;
            if (!depPath.StartsWith("Assets"))          continue;

            string fileName = Path.GetFileName(depPath);
            string newPath  = Path.Combine(targetPath, fileName);

            string error = AssetDatabase.MoveAsset(depPath, newPath);

            if (string.IsNullOrEmpty(error))
                movedCount++;
            else
                Debug.LogWarning($"Could not move {fileName}: {error}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully moved {movedCount} dependencies to {targetPath}");
    }

    #endregion
}