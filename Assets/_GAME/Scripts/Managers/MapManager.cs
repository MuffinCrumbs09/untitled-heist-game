using Unity.Netcode;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

public class MapManager : NetworkBehaviour
{
    public static MapManager Instance;

    [Header("Map Settings")]
    public M_Settings Map;
    public M_Areas[] Areas;
    public M_RandomDialouge MapRandomDialouge;
    public ObjectiveSystem ObjectiveSystem;

    private Dictionary<string, (int min, int max)> _roomLimits = new();
    private Dictionary<string, int> _currentCount = new();
    private List<(string areaName, string roomType)> _activatedRooms = new();

    public NetworkList<NetRoomData> syncedRooms;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;

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
        }
    }

    private void Start()
    {
        if (!IsServer) return;

        int max = 0;
        foreach (Loot loot in GameObject.FindObjectsByType<Loot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            max += loot.LootValue;
            Debug.Log(max);
        }
        NetStore.Instance.SetMaxPayoutServerRpc(max);

        SyncRoomsClientRpc();

        AssignObjectiveTransforms();
        AssignCorrectComputer();
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

    private void AssignCorrectComputer()
    {
        foreach (var objective in ObjectiveSystem.ObjectiveList)
        {
            if (objective.tasks == null) continue;

            foreach (var task in objective.tasks)
            {
                if (task is MinigameTask minigameTask)
                {
                    // Gather all matching computers for this minigame
                    List<Computer> computers = new();
                    List<Transform> foundRooms = FindRoomsByTag(minigameTask.RoomType);

                    foreach (Transform room in foundRooms)
                        FindComputersByRoom(room, ref computers);

                    if (computers.Count == 0) continue;

                    int random = Random.Range(0, computers.Count);
                    Computer selected = computers[random];

                    // Assign locally on host
                    selected.associatedTask = minigameTask;

                    string timerPath = string.Empty;
                    if (selected.type == ComputerType.TIMER)
                    {
                        List<Transform> vaultRooms = FindRoomsByTag("Vault");
                        if (vaultRooms.Count > 0)
                        {
                            DoorTimer timer = FindTimerByRoom(vaultRooms[0]);
                            if (timer != null)
                            {
                                selected.timer = timer;
                                timerPath = GetGameObjectPath(timer.gameObject);
                            }
                        }
                    }

                    // Prepare data to sync to clients
                    string computerPath = GetGameObjectPath(selected.transform.gameObject);
                    string taskName = minigameTask.taskName;

                    // Send RPC to clients to apply the same assignment (use NetString for network serialization)
                    AssignComputersClientRpc(new NetString[] { computerPath }, new NetString[] { taskName }, new NetString[] { timerPath });
                    Debug.Log($"Assigned computer '{computerPath}' -> task '{taskName}'");
                }
            }
        }
    }

    [ClientRpc]
    private void AssignComputersClientRpc(NetString[] computerPaths, NetString[] taskNames, NetString[] timerPaths)
    {
        // Clients will apply assignments received from host
        for (int i = 0; i < computerPaths.Length; i++)
        {
            string compPath = computerPaths[i];
            string taskName = taskNames[i];
            string timerPath = timerPaths[i];

            GameObject compObj = GameObject.Find(compPath);
            if (compObj == null)
                continue;

            if (!compObj.TryGetComponent(out Computer computer))
                continue;

            // Find matching MinigameTask by name in ObjectiveSystem
            if (ObjectiveSystem == null && ObjectiveSystem.Instance != null)
                ObjectiveSystem = ObjectiveSystem.Instance;

            if (ObjectiveSystem != null)
            {
                foreach (var obj in ObjectiveSystem.ObjectiveList)
                {
                    if (obj.tasks == null) continue;

                    foreach (var t in obj.tasks)
                    {
                        if (t is MinigameTask mt && mt.taskName == taskName)
                        {
                            computer.associatedTask = mt;
                            break;
                        }
                    }
                    if (computer.associatedTask != null) break;
                }
            }

            // Assign timer if provided
            if (!string.IsNullOrEmpty(timerPath))
            {
                GameObject timerObj = GameObject.Find(timerPath);
                if (timerObj != null && timerObj.TryGetComponent(out DoorTimer dt))
                {
                    computer.timer = dt;
                }
            }
        }
    }

    public void AssignObjectiveTransforms()
    {
        // Gather all location data to sync to clients
        List<(string taskName, string locationPath)> locationAssignments = new();

        foreach (var objective in ObjectiveSystem.ObjectiveList)
        {
            if (objective.tasks == null) continue;

            foreach (var task in objective.tasks)
            {
                if (task is LocationTask locationTask)
                {
                    if (locationTask.possibleAreas == null || locationTask.possibleAreas.Count == 0)
                    {
                        string roomtag = ExtractRoomTagFromTaskName(locationTask.taskName);

                        if (!string.IsNullOrEmpty(roomtag))
                        {
                            List<Transform> foundRooms = FindRoomsByTag(roomtag);

                            foreach (Transform room in foundRooms)
                            {
                                foreach (M_Locations location in Map.MapLocations)
                                {
                                    if (location.RoomName != roomtag)
                                        continue;

                                    for (int i = 0; i < location.LocationNames.Count; i++)
                                    {
                                        if (room.name == location.LocationNames[i].RoomObjectName)
                                        {
                                            GameObject toSet = GameObject.Find(location.LocationNames[i].LocationObjectName);
                                            locationTask.possibleAreas.Add(toSet.transform);
                                            string locationPath = GetGameObjectPath(toSet);
                                            locationAssignments.Add((locationTask.taskName, locationPath));
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Sync to clients
        if (locationAssignments.Count > 0)
        {
            NetString[] taskNames = locationAssignments.Select(x => (NetString)x.taskName).ToArray();
            NetString[] locationPaths = locationAssignments.Select(x => (NetString)x.locationPath).ToArray();
            AssignObjectiveTransformsClientRpc(taskNames, locationPaths);
        }
    }

    [ClientRpc]
    private void AssignObjectiveTransformsClientRpc(NetString[] taskNames, NetString[] locationPaths)
    {
        // Clients will apply objective location assignments received from host
        for (int i = 0; i < taskNames.Length; i++)
        {
            string taskName = taskNames[i];
            string locationPath = locationPaths[i];

            GameObject locationObj = GameObject.Find(locationPath);
            if (locationObj == null)
                continue;

            // Find matching LocationTask by name in ObjectiveSystem
            if (ObjectiveSystem == null && ObjectiveSystem.Instance != null)
                ObjectiveSystem = ObjectiveSystem.Instance;

            if (ObjectiveSystem != null)
            {
                foreach (var obj in ObjectiveSystem.ObjectiveList)
                {
                    if (obj.tasks == null) continue;

                    foreach (var t in obj.tasks)
                    {
                        if (t is LocationTask lt && lt.taskName == taskName)
                        {
                            lt.possibleAreas.Add(locationObj.transform);
                            break;
                        }
                    }
                }
            }
        }
    }

    #region Networking
    [ClientRpc]
    private void SyncRoomsClientRpc()
    {
        if (IsHost) return;

        foreach (var room in syncedRooms)
        {
            GameObject areaObj = GameObject.Find(room.AreaName);
            for (int i = 0; i < areaObj.transform.childCount; i++)
            {
                Transform child = areaObj.transform.GetChild(i);
                if (child.CompareTag(room.RoomType))
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

    private string ExtractRoomTagFromTaskName(string taskName)
    {
        string lowerTaskName = taskName.ToLower();

        foreach (var roomData in _activatedRooms)
        {
            if (lowerTaskName.Contains(roomData.roomType.ToLower()))
            {
                return roomData.roomType;
            }
        }
        return null;
    }

    private List<Transform> FindRoomsByTag(string roomTag)
    {
        List<Transform> foundRooms = new List<Transform>();

        foreach (var roomData in _activatedRooms)
        {
            if (roomData.roomType.Equals(roomTag, System.StringComparison.OrdinalIgnoreCase))
            {
                GameObject areaObj = GameObject.Find(roomData.areaName);
                if (areaObj != null)
                {
                    for (int i = 0; i < areaObj.transform.childCount; i++)
                    {
                        Transform child = areaObj.transform.GetChild(i);
                        if (child.CompareTag(roomTag) && child.gameObject.activeSelf)
                        {
                            foundRooms.Add(child);
                        }
                    }
                }
            }
        }

        return foundRooms;
    }

    private void FindComputersByRoom(Transform room, ref List<Computer> computers)
    {
        foreach (Transform child in room)
        {
            if (child.TryGetComponent(out Computer computer))
            {
                computers.Add(computer);
            }

            FindComputersByRoom(child, ref computers);
        }
    }

    private DoorTimer FindTimerByRoom(Transform room)
    {
        foreach (Transform child in room)
        {
            if (child.TryGetComponent(out DoorTimer timer))
            {
                return timer;
            }
        }

        return null;
    }

    private string GetGameObjectPath(GameObject obj)
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
}
