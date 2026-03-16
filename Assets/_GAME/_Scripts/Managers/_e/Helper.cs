using System.Collections.Generic;
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
}
