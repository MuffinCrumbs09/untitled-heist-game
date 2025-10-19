using Unity.Netcode;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

public class MapManager : NetworkBehaviour
{
    [Header("Map Settings")]
    public M_Settings Map;
    public M_Areas[] Areas;

    private Dictionary<string, (int min, int max)> _roomLimits = new();
    private Dictionary<string, int> _currentCount = new();
    private List<(string areaName, string roomType)> _activatedRooms = new();

    public NetworkList<NetRoomData> syncedRooms;

    private void Awake()
    {
        syncedRooms = new NetworkList<NetRoomData>();
    }
    public override void OnNetworkSpawn()
    {
        // Disable all rooms
        foreach (M_Areas area in Areas)
        {
            GameObject areaObj = GameObject.Find(area.Area);

            for (int i = 0; i < areaObj.transform.childCount; i++)
            {
                Transform child = areaObj.transform.GetChild(i);

                child.gameObject.SetActive(false);
            }
        }

        if (IsHost)
        {
            // Generate rooms
            AllocateRooms();

            // Send results to clients
            foreach (var (area, room) in _activatedRooms)
            {
                syncedRooms.Add(new NetRoomData(area, room));
            }

            SyncRoomsClientRpc();
        }
    }

    private void AllocateRooms()
    {
        // Get ready
        M_Rooms rooms = Map.MapRooms;
        List<M_Areas> availableAreas = new List<M_Areas>(Areas);
        SetRoomLimits(rooms);

        // Non-Hall rooms

        foreach (var kvp in _roomLimits)
        {
            string roomType = kvp.Key;
            if (roomType.Equals("Hall", System.StringComparison.OrdinalIgnoreCase))
                continue;
            int min = kvp.Value.min;
            int max = kvp.Value.max;

            int roomCount = min > 0 ? Random.Range(min, max + 1) : 0;

            for (int i = 0; i < roomCount; i++)
            {
                int areaRef = -1;
                GameObject room = PickRandomArea(roomType, availableAreas, ref areaRef);
                if (room == null) continue;
                if (_currentCount[room.tag] >= _roomLimits[room.tag].max) continue;

                _currentCount[room.tag]++;
                availableAreas.RemoveAt(areaRef);
                room.SetActive(true);

                string parentAreaName = room.transform.parent.name;
                _activatedRooms.Add((parentAreaName, room.tag));

                ProcessDependencies(parentAreaName, room.tag, availableAreas);
            }
        }

        CheckNonHallAreas(availableAreas);

        // Place halls
        var hallAreas = availableAreas.Where(a => a.CanBeHall).ToList();

        foreach (M_Areas area in hallAreas)
        {
            GameObject areaObj = GameObject.Find(area.Area);
            for (int i = 0; i < areaObj.transform.childCount; i++)
            {
                Transform child = areaObj.transform.GetChild(i);
                if (child.CompareTag("Hall"))
                {
                    child.gameObject.SetActive(true);
                    _activatedRooms.Add((area.Area, "Hall"));
                    break;
                }
            }
        }

        Debug.Log("Rooms Spawned");
        string s = "";
        foreach (var kvp in _activatedRooms)
        {
            s += kvp.areaName + ": " + kvp.roomType + "\n";
        }
        Debug.Log(s);
    }

    private void SetRoomLimits(M_Rooms rooms)
    {
        FieldInfo[] fields = typeof(M_Rooms).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            // X = Min, y = Max
            Vector2 _minMax = (Vector2)field.GetValue(rooms);
            int min = (int)_minMax.x;
            int max = _minMax.y == 0 ? 99 : (int)_minMax.y;

            _roomLimits[field.Name] = (min, max);
            _currentCount[field.Name] = 0;
        }
    }

    private GameObject PickRandomArea(string selectedRoom, List<M_Areas> avaliableAreas, ref int areaIndex)
    {
        const int maxAttempts = 20;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int index = Random.Range(0, avaliableAreas.Count());
            GameObject area = GameObject.Find(avaliableAreas[index].Area);

            for (int i = 0; i < area.transform.childCount; i++)
            {
                Transform child = area.transform.GetChild(i);
                if (child.gameObject.CompareTag(selectedRoom))
                {
                    areaIndex = index;
                    return child.gameObject;
                }
            }
        }

        Debug.LogWarning("Something has gone wrong. Method doesnt work!");
        return null;
    }

    private void ProcessDependencies(string areaName, string roomType, List<M_Areas> avaliableAreas)
    {
        M_Areas areaConfig = Areas.FirstOrDefault(a => a.Area == areaName);

        // if theres no dependencies, return
        if (areaConfig.Dependencies == null || areaConfig.Dependencies.Length == 0)
            return;

        foreach (M_Dependency dependency in areaConfig.Dependencies)
        {
            if (dependency.TriggerRoomType != roomType)
                continue;

            GameObject targetArea = GameObject.Find(dependency.TargetAreaName);
            GameObject exisitingRoom = null;
            string existingRoomType = null;

            for (int i = 0; i < targetArea.transform.childCount; i++)
            {
                Transform child = targetArea.transform.GetChild(i);
                if (child.gameObject.activeSelf)
                {
                    exisitingRoom = child.gameObject;
                    existingRoomType = child.tag;
                    break;
                }
            }

            if (exisitingRoom != null && existingRoomType == dependency.RequiredRoomType)
                continue;

            // Deativate old room
            if (exisitingRoom != null)
            {
                exisitingRoom.SetActive(false);
                _currentCount[existingRoomType]--;

                _activatedRooms.Remove((dependency.TargetAreaName, existingRoomType));
            }

            // If limit has been reached, remove old area and add it back to avaliable
            if (_currentCount[dependency.RequiredRoomType] >= _roomLimits[dependency.RequiredRoomType].max)
            {
                Debug.Log("here");
                var oldType = _activatedRooms.FirstOrDefault(t => t.roomType == dependency.RequiredRoomType && t.areaName == dependency.TargetAreaName);

                // fallback
                if (oldType == default)
                    oldType = _activatedRooms.FirstOrDefault(t => t.roomType == dependency.RequiredRoomType);

                GameObject removeAreaObj = GameObject.Find(oldType.areaName);
                for (int i = 0; i < removeAreaObj.transform.childCount; i++)
                {
                    Transform child = removeAreaObj.transform.GetChild(i);
                    if (child.CompareTag(dependency.RequiredRoomType) && child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                        _currentCount[dependency.RequiredRoomType]--;

                        _activatedRooms.RemoveAll(t => t.areaName == oldType.areaName && t.roomType == oldType.roomType);

                        M_Areas oldArea = Areas.FirstOrDefault(a => a.Area == oldType.areaName);
                        if (oldArea != null && !avaliableAreas.Contains(oldArea))
                            avaliableAreas.Add(oldArea);

                        break;
                    }
                }
            }


            // Activate dependant room
            for (int i = 0; i < targetArea.transform.childCount; i++)
            {
                Transform child = targetArea.transform.GetChild(i);
                if (child.CompareTag(dependency.RequiredRoomType))
                {
                    child.gameObject.SetActive(true);
                    _currentCount[dependency.RequiredRoomType]++;
                    _activatedRooms.Add((dependency.TargetAreaName, dependency.RequiredRoomType));

                    M_Areas trgArea = avaliableAreas.FirstOrDefault(a => a.Area == dependency.TargetAreaName);
                    if (trgArea != null)
                    {
                        avaliableAreas.Remove(trgArea);
                    }
                }
            }
        }
    }

    #region Networking
    [ClientRpc]
    private void SyncRoomsClientRpc()
    {
        foreach (var room in syncedRooms)
        {
            GameObject areaObj = GameObject.Find(room.AreaName);
            for(int i = 0; i < areaObj.transform.childCount; i++)
            {
                Transform child = areaObj.transform.GetChild(i);
                if(child.CompareTag(room.RoomType))
                {
                    child.gameObject.SetActive(true);
                    break;
                }
            }
        }
    }
    #endregion

    #region Helper
    private void CheckNonHallAreas(List<M_Areas> availableAreas)
    {
        var emptyNonHallAreas = availableAreas.Where(a => !a.CanBeHall).ToList();

        foreach (M_Areas area in emptyNonHallAreas)
        {
            GameObject areaObj = GameObject.Find(area.Area);
            bool hasActiveRoom = false;

            for (int i = 0; i < areaObj.transform.childCount; i++)
            {
                if (areaObj.transform.GetChild(i).gameObject.activeSelf)
                {
                    hasActiveRoom = true;
                    break;
                }
            }

            if (!hasActiveRoom)
            {
                string selectedRoomType = area.Rooms[Random.Range(0, area.Rooms.Length)];

                for (int i = 0; i < areaObj.transform.childCount; i++)
                {
                    Transform child = areaObj.transform.GetChild(i);
                    if (child.CompareTag(selectedRoomType))
                    {
                        if (_currentCount.ContainsKey(selectedRoomType) &&
                            _currentCount[selectedRoomType] < _roomLimits[selectedRoomType].max)
                        {
                            child.gameObject.SetActive(true);
                            _currentCount[selectedRoomType]++;
                            _activatedRooms.Add((area.Area, selectedRoomType));
                            availableAreas.Remove(area);
                            break;
                        }
                    }
                }
            }
        }
    }
    #endregion
}
