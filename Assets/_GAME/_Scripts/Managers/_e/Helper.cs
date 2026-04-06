using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public static class Helper
{
    /// <summary> Recursively searches a room's hierarchy for ItemType components that match the given tag and adds their GameObjects to the list. </summary>
    /// <param name="room">The room transform to search within.</param>
    /// <param name="itemType">The type of item to search for.</param>
    /// <param name="items">The list to add found items to.</param>
    public static void FindItemsByRoom(Transform room, ItemTypeTag itemType, ref List<GameObject> items)
    {
        foreach (Transform child in room)
        {
            if (child.TryGetComponent(out ItemType item) && item.Tag == itemType)
                items.Add(child.gameObject);

            FindItemsByRoom(child, itemType, ref items); // Recurse into nested children
        }
    }

    /// <summary>
    /// Builds a full scene hierarchy path for a GameObject (e.g. "Root/Parent/Child/Object").
    /// Used for passing object references across the network, since NetworkObject paths are stable.
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

    /// <summary>
    /// Finds the room transform that contains the Computer associated with the given minigame task.
    /// Starts by locating the task in the objective list (by index), then finds the matching Computer
    /// and walks up the hierarchy until it finds a GameObject with a RoomType component.
    /// </summary>
    public static Transform GoToTaskRoom(int objectiveIndex, int taskIndex = 0)
    {
        if (ObjectiveSystem.Instance == null) return null;
        if (objectiveIndex < 0 || objectiveIndex >= ObjectiveSystem.Instance.ObjectiveList.Count) return null;

        var objective = ObjectiveSystem.Instance.ObjectiveList[objectiveIndex];
        if (objective.tasks == null || taskIndex < 0 || taskIndex >= objective.tasks.Count) return null;

        if (objective.tasks[taskIndex] is not MinigameTask minigameTask) return null;

        // Find the computer that is currently associated with this task (assigned in MapManager).
        // Use FindObjectsByType so we don't rely on deprecated APIs.
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

        // Walk up the hierarchy until we find a room (identified by a RoomType component).
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
    /// Retrieves the Computer component associated with a specific MinigameTask.
    /// Looks up the task by objective index and task index, then finds its assigned Computer.
    /// </summary>
    public static Computer GetComputerFromTask(int objectiveIndex, int taskIndex = 0)
    {
        if (ObjectiveSystem.Instance == null) return null;
        if (objectiveIndex < 0 || objectiveIndex >= ObjectiveSystem.Instance.ObjectiveList.Count) return null;

        var objective = ObjectiveSystem.Instance.ObjectiveList[objectiveIndex];
        if (objective.tasks == null || taskIndex < 0 || taskIndex >= objective.tasks.Count) return null;

        if (objective.tasks[taskIndex] is not MinigameTask minigameTask) return null;

        // Find the computer that is currently associated with this task (assigned in MapManager).
        foreach (var computer in GameObject.FindObjectsByType<Computer>(FindObjectsSortMode.None))
        {
            if (computer.associatedTask == minigameTask)
                return computer;
        }

        return null;
    }

    /// <summary>
    /// Shuffles a list of gameobjects. Useful for map generation to ensure randomness
    /// </summary>
    public static void ShuffleList(ref List<GameObject> list)
    {
        for (int s = 0; s < list.Count - 1; s++)
        {
            int newIndex = UnityEngine.Random.Range(s, list.Count);

            var toSwap = list[s];
            list[s] = list[newIndex];
            list[newIndex] = toSwap;
        }
    }

    /// <summary>
    /// Get the current objective and taask index
    /// </summary>
    /// <returns>Vector2(ObjectiveIndex, TaskIndex)
    /// Returns default value of (-1, -1) if an error has occured</returns>
    public static Vector2 GetCurrentObjectiveAndTaskIndex()
    {
        ObjectiveSystem system = ObjectiveSystem.Instance;

        if (system == null)
            return new Vector2(-1, -1);

        int x = system.CurrentObjectiveIndex.Value;
        int y = system.ObjectiveList[x].GetCurrentTaskIndex();
        return new Vector2(x, y);
    }

    public static string ToHex(this Color c)
    {
        return string.Format("#{0:X2}{1:X2}{2:X2}", ToByte(c.r), ToByte(c.g), ToByte(c.b));
    }

    public static byte ToByte(float f)
    {
        f = Mathf.Clamp01(f);
        return (byte)(f * 255);
    }

    /// <summary>
    /// Outputs text in desired colour for debug.logs
    /// </summary>
    /// <returns>Colour string</returns>
    public static string Color(this string text, Color color)
    {
        string output;
        output = string.Format("<color={0}>{1}</color>", color.ToHex(), text);
        return output;
    }
}
