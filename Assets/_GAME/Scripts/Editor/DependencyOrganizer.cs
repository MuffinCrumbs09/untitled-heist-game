using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class DependencyOrganizer : EditorWindow
{
    [MenuItem("Assets/Organize Prefab Dependencies")]
    public static void OrganizeDependencies()
    {
        // Get the selected object
        GameObject selectedPrefab = Selection.activeObject as GameObject;

        if (selectedPrefab == null || PrefabUtility.GetPrefabAssetType(selectedPrefab) == PrefabAssetType.NotAPrefab)
        {
            Debug.LogError("Please select a Prefab in the Project window first.");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
        string rootPath = Path.GetDirectoryName(prefabPath);
        string newFolderName = selectedPrefab.name + "_Dependencies";
        string targetFolderPath = Path.Combine(rootPath, newFolderName);

        // Create the target folder
        if (!AssetDatabase.IsValidFolder(targetFolderPath))
        {
            AssetDatabase.CreateFolder(rootPath, newFolderName);
        }

        // Find all dependencies
        Object[] dependencies = EditorUtility.CollectDependencies(new Object[] { selectedPrefab });
        
        int movedCount = 0;

        foreach (Object dep in dependencies)
        {
            string depPath = AssetDatabase.GetAssetPath(dep);

            // Skip if the dependency is the prefab itself, a script, or a built-in resource
            if (depPath == prefabPath || dep is MonoScript || depPath.StartsWith("Resources/") || depPath.StartsWith("Library/"))
                continue;

            // Only move files that are actually inside the Assets folder
            if (!string.IsNullOrEmpty(depPath) && depPath.StartsWith("Assets"))
            {
                string fileName = Path.GetFileName(depPath);
                string newPath = Path.Combine(targetFolderPath, fileName);

                // Move the asset
                string error = AssetDatabase.MoveAsset(depPath, newPath);

                if (string.IsNullOrEmpty(error))
                    movedCount++;
                else
                    Debug.LogWarning($"Could not move {fileName}: {error}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Successfully moved {movedCount} dependencies to {targetFolderPath}");
    }
}