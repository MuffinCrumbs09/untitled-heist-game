using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Collection of reusable utility functions used across the project.
/// Includes hierarchy searching, objective helpers, list utilities, and formatting.
/// </summary>
public static class Helper
{
    #region Item Search

    /// <summary>
    /// Recursively searches a room hierarchy for items matching a specific tag.
    /// </summary>
    public static void FindItemsByRoom(Transform room, ItemTypeTag itemType, ref List<GameObject> items)
    {
        foreach (Transform child in room)
        {
            if (child.TryGetComponent(out ItemType item) && item.Tag == itemType)
                items.Add(child.gameObject);

            FindItemsByRoom(child, itemType, ref items);
        }
    }

    #endregion

    #region Scene / Hierarchy Helpers

    /// <summary>
    /// Builds a full hierarchy path for a GameObject.
    /// Useful for networking or debugging.
    /// </summary>
    public static string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return string.Empty;

        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    #endregion

    #region Objective & Task Helpers

    /// <summary>
    /// Finds the room that contains the computer for a given task.
    /// </summary>
    public static Transform GoToTaskRoom(int objectiveIndex, int taskIndex = 0)
    {
        if (ObjectiveSystem.Instance == null) return null;
        if (objectiveIndex < 0 || objectiveIndex >= ObjectiveSystem.Instance.ObjectiveList.Count) return null;

        var objective = ObjectiveSystem.Instance.ObjectiveList[objectiveIndex];
        if (objective.tasks == null || taskIndex < 0 || taskIndex >= objective.tasks.Count) return null;

        if (objective.tasks[taskIndex] is not MinigameTask minigameTask) return null;

        Computer computer = null;

        foreach (var c in GameObject.FindObjectsByType<Computer>(FindObjectsSortMode.None))
        {
            if (c.associatedTask == minigameTask)
            {
                computer = c;
                break;
            }
        }

        if (computer == null) return null;

        Transform current = computer.transform;

        while (current != null)
        {
            if (current.TryGetComponent<RoomType>(out var roomType) && roomType.Tag != null)
                return current;

            current = current.parent;
        }

        return null;
    }

    /// <summary>
    /// Gets the computer linked to a task.
    /// </summary>
    public static Computer GetComputerFromTask(int objectiveIndex, int taskIndex = 0)
    {
        if (ObjectiveSystem.Instance == null) return null;
        if (objectiveIndex < 0 || objectiveIndex >= ObjectiveSystem.Instance.ObjectiveList.Count) return null;

        var objective = ObjectiveSystem.Instance.ObjectiveList[objectiveIndex];
        if (objective.tasks == null || taskIndex < 0 || taskIndex >= objective.tasks.Count) return null;

        if (objective.tasks[taskIndex] is not MinigameTask minigameTask) return null;

        foreach (var computer in GameObject.FindObjectsByType<Computer>(FindObjectsSortMode.None))
        {
            if (computer.associatedTask == minigameTask)
                return computer;
        }

        return null;
    }

    /// <summary>
    /// Returns current objective and task index.
    /// </summary>
    public static Vector2 GetCurrentObjectiveAndTaskIndex()
    {
        ObjectiveSystem system = ObjectiveSystem.Instance;

        if (system == null)
            return new Vector2(-1, -1);

        int x = system.CurrentObjectiveIndex.Value;
        int y = system.ObjectiveList[x].GetCurrentTaskIndex();

        return new Vector2(x, y);
    }

    #endregion

    #region List Utilities

    /// <summary>
    /// Randomly shuffles a list.
    /// </summary>
    public static void ShuffleList(ref List<GameObject> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);

            var temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    #endregion

    #region Color & Formatting

    /// <summary>
    /// Converts a Color to HEX string.
    /// </summary>
    public static string ToHex(this Color c)
    {
        return $"#{ToByte(c.r):X2}{ToByte(c.g):X2}{ToByte(c.b):X2}";
    }

    private static byte ToByte(float f)
    {
        return (byte)(Mathf.Clamp01(f) * 255);
    }

    /// <summary>
    /// Wraps text in color tags for Debug logs.
    /// </summary>
    public static string Color(this string text, Color color)
    {
        return $"<color={color.ToHex()}>{text}</color>";
    }

    #endregion
}