using Unity.Netcode;
using UnityEngine;
using Unity.AI.Navigation;
using System.Collections;
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

    [Header("Room Tags")]
    [SerializeField] private RoomTypeTag _hallTag;

    [Header("NavMesh")]
    [SerializeField] private NavMeshSurface _navMeshSurface;
    public event System.Action OnNavMeshReady;

    private Dictionary<RoomTypeTag, (int min, int max)> _roomLimits = new();
    private Dictionary<RoomTypeTag, int> _currentCount = new();
    private List<(string areaName, RoomTypeTag roomType)> _activatedRooms = new();

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
                syncedRooms.Add(new NetRoomData(area, room.name));
            }
            // Bake NavMesh AFTER roooms are spawned
            StartCoroutine(BakeNavMeshThenInit());
        }
    }

    private void Start()
    {
        if (!IsServer) return;
        OnNavMeshReady += OnNavMeshBaked;
    }

    private void OnNavMeshBaked()
    {
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
        M_Rooms rooms = Map.MapRooms;
        List<M_Areas> availableAreas = new List<M_Areas>(Areas);
        SetRoomLimits(rooms);

        // Non-Hall rooms
        foreach (var kvp in _roomLimits)
        {
            RoomTypeTag roomTag = kvp.Key;
            if (roomTag == _hallTag) continue;

            int min = kvp.Value.min;
            int max = kvp.Value.max;
            int roomCount = min > 0 ? Random.Range(min, max + 1) : 0;

            for (int i = 0; i < roomCount; i++)
            {
                int areaRef = -1;
                GameObject room = PickRandomArea(roomTag, availableAreas, ref areaRef);
                if (room == null) continue;

                RoomTypeTag roomType = GetRoomType(room.transform);
                if (roomType == null || _currentCount[roomType] >= _roomLimits[roomType].max) continue;

                _currentCount[roomType]++;
                availableAreas.RemoveAt(areaRef);
                room.SetActive(true);

                string parentAreaName = room.transform.parent.name;
                _activatedRooms.Add((parentAreaName, roomType));

                ProcessDependencies(parentAreaName, roomType, availableAreas);
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
                if (IsRoomType(child, _hallTag))
                {
                    child.gameObject.SetActive(true);
                    _activatedRooms.Add((area.Area, _hallTag));
                    break;
                }
            }
        }

        Debug.Log("Rooms Spawned");
        string s = "";
        foreach (var kvp in _activatedRooms)
        {
            s += kvp.areaName + ": " + kvp.roomType.name + "\n";
        }
        Debug.Log(s);
    }

    private void SetRoomLimits(M_Rooms rooms)
    {
        foreach (RoomTypeLimit limit in rooms.Limits)
        {
            int min = (int)limit.MinMax.x;
            int max = limit.MinMax.y == 0 ? 99 : (int)limit.MinMax.y;

            _roomLimits[limit.RoomType] = (min, max);
            _currentCount[limit.RoomType] = 0;
        }
    }

    private GameObject PickRandomArea(RoomTypeTag selectedRoom, List<M_Areas> availableAreas, ref int areaIndex)
    {
        const int maxAttempts = 20;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int index = Random.Range(0, availableAreas.Count);
            GameObject area = GameObject.Find(availableAreas[index].Area);

            for (int i = 0; i < area.transform.childCount; i++)
            {
                Transform child = area.transform.GetChild(i);
                if (IsRoomType(child, selectedRoom))
                {
                    areaIndex = index;
                    return child.gameObject;
                }
            }
        }

        Debug.LogWarning($"Could not find a valid area for room type '{selectedRoom.name}'.");
        return null;
    }

    private void ProcessDependencies(string areaName, RoomTypeTag roomType, List<M_Areas> availableAreas)
    {
        M_Areas areaConfig = Areas.FirstOrDefault(a => a.Area == areaName);

        if (areaConfig.Dependencies == null || areaConfig.Dependencies.Length == 0)
            return;

        foreach (M_Dependency dependency in areaConfig.Dependencies)
        {
            if (dependency.TriggerRoomType != roomType)
                continue;

            GameObject targetArea = GameObject.Find(dependency.TargetAreaName);
            GameObject existingRoom = null;
            RoomTypeTag existingRoomType = null;

            for (int i = 0; i < targetArea.transform.childCount; i++)
            {
                Transform child = targetArea.transform.GetChild(i);
                if (child.gameObject.activeSelf)
                {
                    existingRoom = child.gameObject;
                    existingRoomType = GetRoomType(child);
                    break;
                }
            }

            if (existingRoom != null && existingRoomType == dependency.RequiredRoomType)
                continue;

            // Deactivate old room
            if (existingRoom != null)
            {
                existingRoom.SetActive(false);
                _currentCount[existingRoomType]--;
                _activatedRooms.Remove((dependency.TargetAreaName, existingRoomType));
            }

            // If limit reached, free up an existing room of the required type
            if (_currentCount[dependency.RequiredRoomType] >= _roomLimits[dependency.RequiredRoomType].max)
            {
                Debug.Log("Limit reached — freeing room to satisfy dependency.");
                var oldEntry = _activatedRooms.FirstOrDefault(t =>
                    t.roomType == dependency.RequiredRoomType && t.areaName == dependency.TargetAreaName);

                if (oldEntry == default)
                    oldEntry = _activatedRooms.FirstOrDefault(t => t.roomType == dependency.RequiredRoomType);

                GameObject removeAreaObj = GameObject.Find(oldEntry.areaName);
                for (int i = 0; i < removeAreaObj.transform.childCount; i++)
                {
                    Transform child = removeAreaObj.transform.GetChild(i);
                    if (IsRoomType(child, dependency.RequiredRoomType) && child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                        _currentCount[dependency.RequiredRoomType]--;
                        _activatedRooms.RemoveAll(t => t.areaName == oldEntry.areaName && t.roomType == oldEntry.roomType);

                        M_Areas oldArea = Areas.FirstOrDefault(a => a.Area == oldEntry.areaName);
                        if (oldArea != null && !availableAreas.Contains(oldArea))
                            availableAreas.Add(oldArea);

                        break;
                    }
                }
            }

            // Activate dependent room
            for (int i = 0; i < targetArea.transform.childCount; i++)
            {
                Transform child = targetArea.transform.GetChild(i);
                if (IsRoomType(child, dependency.RequiredRoomType))
                {
                    child.gameObject.SetActive(true);
                    _currentCount[dependency.RequiredRoomType]++;
                    _activatedRooms.Add((dependency.TargetAreaName, dependency.RequiredRoomType));

                    M_Areas trgArea = availableAreas.FirstOrDefault(a => a.Area == dependency.TargetAreaName);
                    if (trgArea != null)
                        availableAreas.Remove(trgArea);
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
                    List<Computer> computers = new();
                    List<Transform> foundRooms = FindRoomsByTag(minigameTask.RoomType);

                    foreach (Transform room in foundRooms)
                        FindComputersByRoom(room, ref computers);

                    if (computers.Count == 0) continue;

                    int random = Random.Range(0, computers.Count);
                    Computer selected = computers[random];

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

                    string computerPath = GetGameObjectPath(selected.transform.gameObject);
                    string taskName = minigameTask.taskName;

                    AssignComputersClientRpc(new NetString[] { computerPath }, new NetString[] { taskName }, new NetString[] { timerPath });
                    Debug.Log($"Assigned computer '{computerPath}' -> task '{taskName}'");
                }
            }
        }
    }

    [ClientRpc]
    private void AssignComputersClientRpc(NetString[] computerPaths, NetString[] taskNames, NetString[] timerPaths)
    {
        for (int i = 0; i < computerPaths.Length; i++)
        {
            string compPath = computerPaths[i];
            string taskName = taskNames[i];
            string timerPath = timerPaths[i];

            GameObject compObj = GameObject.Find(compPath);
            if (compObj == null) continue;

            if (!compObj.TryGetComponent(out Computer computer)) continue;

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

            if (!string.IsNullOrEmpty(timerPath))
            {
                GameObject timerObj = GameObject.Find(timerPath);
                if (timerObj != null && timerObj.TryGetComponent(out DoorTimer dt))
                    computer.timer = dt;
            }
        }
    }

    public void AssignObjectiveTransforms()
    {
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
        for (int i = 0; i < taskNames.Length; i++)
        {
            string taskName = taskNames[i];
            string locationPath = locationPaths[i];

            GameObject locationObj = GameObject.Find(locationPath);
            if (locationObj == null) continue;

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
            if (areaObj == null) continue;

            for (int i = 0; i < areaObj.transform.childCount; i++)
            {
                Transform child = areaObj.transform.GetChild(i);
                RoomType rt = child.GetComponent<RoomType>();
                if (rt != null && rt.Tag != null && rt.Tag.name == (string)room.RoomType)
                {
                    child.gameObject.SetActive(true);
                    break;
                }
            }
        }
    }
    #endregion

    #region Helpers
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
                RoomTypeTag selectedRoomType = area.Rooms[Random.Range(0, area.Rooms.Length)];

                for (int i = 0; i < areaObj.transform.childCount; i++)
                {
                    Transform child = areaObj.transform.GetChild(i);
                    if (IsRoomType(child, selectedRoomType))
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
            if (lowerTaskName.Contains(roomData.roomType.name.ToLower()))
                return roomData.roomType.name;
        }

        return null;
    }

    /// <summary>
    /// Finds all active room transforms matching the given room type tag name.
    /// </summary>
    private List<Transform> FindRoomsByTag(string roomTagName)
    {
        List<Transform> foundRooms = new List<Transform>();

        foreach (var roomData in _activatedRooms)
        {
            if (!roomData.roomType.name.Equals(roomTagName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            GameObject areaObj = GameObject.Find(roomData.areaName);
            if (areaObj == null) continue;

            for (int i = 0; i < areaObj.transform.childCount; i++)
            {
                Transform child = areaObj.transform.GetChild(i);
                if (IsRoomType(child, roomData.roomType) && child.gameObject.activeSelf)
                    foundRooms.Add(child);
            }
        }

        return foundRooms;
    }

    private void FindComputersByRoom(Transform room, ref List<Computer> computers)
    {
        foreach (Transform child in room)
        {
            if (child.TryGetComponent(out Computer computer))
                computers.Add(computer);

            FindComputersByRoom(child, ref computers);
        }
    }

    private DoorTimer FindTimerByRoom(Transform room)
    {
        foreach (Transform child in room)
        {
            if (child.TryGetComponent(out DoorTimer timer))
                return timer;
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

    private static bool IsRoomType(Transform t, RoomTypeTag tag) =>
        t.TryGetComponent<RoomType>(out RoomType rt) && rt.Tag == tag;

    private static RoomTypeTag GetRoomType(Transform t) =>
        t.TryGetComponent<RoomType>(out RoomType rt) ? rt.Tag : null;

    private IEnumerator BakeNavMeshThenInit()
    {
        AsyncOperation bake = _navMeshSurface.UpdateNavMesh(_navMeshSurface.navMeshData);
        yield return bake;

        Debug.Log("[MapManager] NavMesh baked successfully.");
        OnNavMeshReady?.Invoke();
    }
    #endregion
}
