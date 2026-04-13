using Unity.Netcode;
using UnityEngine;
using Unity.AI.Navigation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class MapManager : NetworkBehaviour
{
    public static MapManager Instance;

    #region STATE MACHINE
    public enum MapState
    {
        None,
        GeneratingRooms,
        SyncingRooms,
        BakingNavMesh,
        SpawningWorld,
        AssigningObjectives,
        Completed
    }

    public MapState CurrentState { get; private set; } = MapState.None;
    public event System.Action<MapState> OnStateChanged;

    private void SetState(MapState newState)
    {
        CurrentState = newState;
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] State -> {newState}", this);
#endif
        OnStateChanged?.Invoke(newState);
    }
    #endregion

    #region Inspector
    public M_Settings Map;
    public M_Areas[] Areas;
    public RandomObjectiveData[] RandomObjectives;
    public ObjectiveHintData[] ObjectiveHints;
    public M_RandomDialouge MapRandomDialouge;
    public ObjectiveSystem ObjectiveSystem;

    [SerializeField] private UnityEvent OnMapGenerated;
    [SerializeField] private RoomTypeTag _hallTag;
    [SerializeField] private NavMeshSurface _navMeshSurface;

    [System.Serializable]
    public class AlwaysSpawnedRoom
    {
        [Tooltip("The area name this room belongs to (e.g. the parent GameObject name)")]
        public string areaName;
        [Tooltip("The RoomTypeTag of this room")]
        public RoomTypeTag roomType;
    }
    [Header("Always Spawned Rooms")]
    [Tooltip("Rooms that are always present in the scene and should be treated as activated")]
    [SerializeField] private List<AlwaysSpawnedRoom> _alwaysSpawnedRooms = new();
    #endregion

    #region Data
    private Dictionary<RoomTypeTag, (int min, int max)> _roomLimits = new();
    private Dictionary<RoomTypeTag, int> _currentCount = new();
    private List<(string areaName, RoomTypeTag roomType)> _activatedRooms = new();

    public NetworkList<NetRoomData> syncedRooms;
    public NetworkList<NetString> syncedRandomObjectives;
    #endregion

    #region Unity
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;

        syncedRooms = new NetworkList<NetRoomData>();
        syncedRandomObjectives = new NetworkList<NetString>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, "[MapManager] OnNetworkSpawn — server starting MapFlow.", this);
#endif
        StartCoroutine(MapFlow());
    }
    #endregion

    #region FLOW
    private IEnumerator MapFlow()
    {
        SetState(MapState.GeneratingRooms);
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, "[MapManager] Beginning room allocation.", this);
#endif
        AllocateRooms();
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Room allocation complete. {_activatedRooms.Count} room(s) activated.", this);
#endif
        yield return null;

        SetState(MapState.SyncingRooms);
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Syncing {_activatedRooms.Count} room(s) to NetworkList.", this);
#endif
        foreach (var (area, room) in _activatedRooms)
            syncedRooms.Add(new NetRoomData(area, room.name));
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] syncedRooms now contains {syncedRooms.Count} entries.", this);
#endif
        yield return null;

        SetState(MapState.BakingNavMesh);
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, "[MapManager] Starting NavMesh bake.", this);
#endif
        yield return _navMeshSurface.UpdateNavMesh(_navMeshSurface.navMeshData);
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, "[MapManager] NavMesh bake complete.", this);
#endif

        SetState(MapState.SpawningWorld);
        yield return null;
        yield return new WaitForSeconds(0.1f);

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, "[MapManager] Spawning random objectives.", this);
#endif
        SpawnRandomObjectivesNew();

        SetState(MapState.AssigningObjectives);

        int max = 0;

        foreach (Loot loot in GameObject.FindObjectsByType<Loot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (loot.TryGetComponent<MeshRenderer>(out var renderer) && renderer.enabled)
            {
                max += loot.LootValue;
            }
        }

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Total max payout calculated (visible meshes only): {max}.", this);
#endif

        NetStore.Instance.SetMaxPayoutServerRpc(max);

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, "[MapManager] Assigning objective transforms.", this);
#endif
        AssignObjectiveTransforms();
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, "[MapManager] Assigning correct computers.", this);
#endif
        AssignCorrectComputer();

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, "[MapManager] Spawning objective hints.", this);
#endif
        SpawnObjectiveHints();

        SetState(MapState.Completed);
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, "[MapManager] Map generation fully complete. Invoking OnMapGenerated.", this);
#endif
        OnMapGenerated?.Invoke();
    }
    #endregion

    #region MAP GENERATION
    private void AllocateRooms()
    {
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Registering {_alwaysSpawnedRooms.Count} always-spawned room(s).", this);
#endif
        foreach (var entry in _alwaysSpawnedRooms)
        {
            if (entry.roomType == null) continue;
            _activatedRooms.Add((entry.areaName, entry.roomType));
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Always-spawned room registered: area='{entry.areaName}', type='{entry.roomType.name}'.", this);
#endif
        }

        M_Rooms rooms = Map.MapRooms;
        List<M_Areas> availableAreas = new List<M_Areas>(Areas);
        SetRoomLimits(rooms);

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Starting room allocation loop. {_roomLimits.Count} room type limit(s) configured. {availableAreas.Count} area(s) available.", this);
#endif

        foreach (var kvp in _roomLimits)
        {
            RoomTypeTag roomTag = kvp.Key;
            if (roomTag == _hallTag) continue;

            int min = kvp.Value.min;
            int max = kvp.Value.max;
            int count = min >= 0 ? Random.Range(min, max + 1) : 0;

#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] RoomType '{roomTag.name}': min={min}, max={max}, rolling count={count}.", this);
#endif

            for (int i = 0; i < count; i++)
            {
                int idx = -1;
                GameObject room = PickRandomArea(roomTag, availableAreas, ref idx);
                if (room == null)
                {
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] No available area found for room type '{roomTag.name}' on iteration {i}. Skipping.", this);
#endif
                    continue;
                }

                RoomTypeTag type = GetRoomType(room.transform);
                if (type == null || _currentCount[type] >= _roomLimits[type].max)
                {
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Room '{room.name}' skipped — type null or count at max for '{type?.name}'.", this);
#endif
                    continue;
                }

                _currentCount[type]++;
                availableAreas.RemoveAt(idx);
                ShowRoom(room.transform);

                string area = room.transform.parent.name;
                _activatedRooms.Add((area, type));

#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Activated room '{room.name}' (type='{type.name}') in area '{area}'. Count for type: {_currentCount[type]}.", this);
#endif

                ProcessDependencies(area, type, availableAreas);
            }
        }

        CheckNonHallAreas(availableAreas);

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Filling remaining {availableAreas.Count(a => a.CanBeHall)} hall-eligible area(s).", this);
#endif

        foreach (var area in availableAreas.Where(a => a.CanBeHall))
        {
            GameObject obj = GameObject.Find(area.Area);
            foreach (Transform child in obj.transform)
            {
                if (IsRoomType(child, _hallTag))
                {
                    ShowRoom(child);
                    _activatedRooms.Add((area.Area, _hallTag));
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Hall room '{child.name}' activated in area '{area.Area}'.", this);
#endif
                    break;
                }
            }
        }
    }

    private void SetRoomLimits(M_Rooms rooms)
    {
        foreach (RoomTypeLimit limit in rooms.Limits)
        {
            int min = (int)limit.MinMax.x;
            int max = limit.MinMax.y == 0 ? 99 : (int)limit.MinMax.y;
            _roomLimits[limit.RoomType] = (min, max);
            _currentCount[limit.RoomType] = 0;
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Room limit set: type='{limit.RoomType.name}', min={min}, max={max}.", this);
#endif
        }
    }

    private GameObject PickRandomArea(RoomTypeTag tag, List<M_Areas> areas, ref int index)
    {
        for (int i = 0; i < 20; i++)
        {
            int idx = Random.Range(0, areas.Count);
            GameObject area = GameObject.Find(areas[idx].Area);

            foreach (Transform child in area.transform)
            {
                if (IsRoomType(child, tag))
                {
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] PickRandomArea found '{child.name}' for tag '{tag.name}' in area '{areas[idx].Area}' on attempt {i + 1}.", this);
#endif
                    index = idx;
                    return child.gameObject;
                }
            }
        }

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] PickRandomArea failed to find a room for tag '{tag.name}' after 20 attempts.", this);
#endif
        return null;
    }

    private void ProcessDependencies(string areaName, RoomTypeTag roomType, List<M_Areas> availableAreas)
    {
        var area = Areas.FirstOrDefault(a => a.Area == areaName);
        if (area?.Dependencies == null) return;

        foreach (var dep in area.Dependencies)
        {
            if (dep.TriggerRoomType != roomType) continue;

#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Processing dependency in area '{areaName}': trigger='{roomType.name}', required='{dep.RequiredRoomType.name}', target='{dep.TargetAreaName}'.", this);
#endif

            GameObject target = GameObject.Find(dep.TargetAreaName);

            foreach (Transform child in target.transform)
            {
                if (IsRoomType(child, dep.RequiredRoomType))
                {
                    ShowRoom(child);
                    _currentCount[dep.RequiredRoomType]++;
                    _activatedRooms.Add((dep.TargetAreaName, dep.RequiredRoomType));
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Dependency satisfied: activated '{child.name}' (type='{dep.RequiredRoomType.name}') in '{dep.TargetAreaName}'.", this);
#endif
                    break;
                }
            }
        }
    }

    private void CheckNonHallAreas(List<M_Areas> areas)
    {
        var nonHallAreas = areas.Where(a => !a.CanBeHall).ToList();
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] CheckNonHallAreas: processing {nonHallAreas.Count} non-hall area(s).", this);
#endif

        foreach (var area in nonHallAreas)
        {
            GameObject obj = GameObject.Find(area.Area);
            RoomTypeTag type = area.Rooms[Random.Range(0, area.Rooms.Length)];

            foreach (Transform child in obj.transform)
            {
                if (IsRoomType(child, type))
                {
                    ShowRoom(child);
                    _currentCount[type]++;
                    _activatedRooms.Add((area.Area, type));
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Non-hall area '{area.Area}' filled with room '{child.name}' (type='{type.name}').", this);
#endif
                    break;
                }
            }
        }
    }
    #endregion

    #region OBJECTIVES
    private void SpawnRandomObjectivesNew()
    {
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] SpawnRandomObjectivesNew: processing {RandomObjectives.Length} objective data entry(s).", this);
#endif

        foreach (var data in RandomObjectives)
        {
            var rooms = FindRoomsByTag(data.RequiredRoomType.name);
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Objective type '{data.SpawnItemType}' requires room '{data.RequiredRoomType.name}': {rooms.Count} matching room(s) found.", this);
#endif

            foreach (var room in rooms)
            {
                List<GameObject> items = new();
                Helper.FindItemsByRoom(room, data.SpawnItemType, ref items);

                int count = Mathf.Min(data.GetRandomSpawnCount(), items.Count);
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Room '{room.name}': {items.Count} candidate(s) found, spawning {count} objective(s) of type '{data.SpawnItemType}'.", this);
#endif

                for (int i = 0; i < count; i++)
                    if (items[i].TryGetComponent(out RandomObject obj))
                        obj.ChangeStateRpc(true);
            }
        }
    }

    private void SpawnObjectiveHints()
    {
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] SpawnObjectiveHints: processing {ObjectiveHints.Length} hint entry(s).", this);
#endif

        foreach (var data in ObjectiveHints)
        {
            Transform room = Helper.GoToTaskRoom(data.Index.x, data.Index.y);
            if (room == null)
            {
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Hint index ({data.Index.x},{data.Index.y}): room not found, skipping.", this);
#endif
                continue;
            }

            List<GameObject> items = new();
            Helper.FindItemsByRoom(room, data.SpawnItemType, ref items);
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Hint in room '{room.name}': activating {items.Count} hint item(s) of type '{data.SpawnItemType}'.", this);
#endif

            foreach (var item in items)
                if (item.TryGetComponent(out RandomObject obj))
                    obj.ChangeStateRpc(true);
        }
    }

    /// <summary>
    /// Server-side: finds world transforms for LocationTask objectives by matching room names
    /// to entries in the map's location data, then sends the paths to all clients.
    /// </summary>
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
                    // Only process tasks that don't already have assigned areas
                    if (locationTask.possibleAreas == null || locationTask.possibleAreas.Count == 0)
                    {
#if UNITY_EDITOR
                        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Processing LocationTask: '{locationTask.taskName}', possibleAreas count: {locationTask.possibleAreas?.Count ?? -1}", this);
#endif

                        // Derive the room type name from the task name (e.g. "Go to Vault" → "Vault")
                        string roomtag = ExtractRoomTagFromTaskName(locationTask.taskName);

#if UNITY_EDITOR
                        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Extracted roomtag: '{roomtag ?? "NULL"}' from task '{locationTask.taskName}'", this);
                        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] _activatedRooms contains: {string.Join(", ", _activatedRooms.Select(r => $"{r.areaName}:{r.roomType.name}"))}", this);
#endif

                        if (!string.IsNullOrEmpty(roomtag))
                        {
                            List<Transform> foundRooms = FindRoomsByTag(roomtag);

#if UNITY_EDITOR
                            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] FindRoomsByTag('{roomtag}') returned {foundRooms.Count} room(s)", this);
#endif

                            foreach (Transform room in foundRooms)
                            {
#if UNITY_EDITOR
                                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Evaluating room transform: '{room.name}'", this);
#endif

                                // Match the active room to a configured location entry
                                foreach (M_Locations location in Map.MapLocations)
                                {
                                    if (location.RoomName != roomtag)
                                    {
#if UNITY_EDITOR
                                        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Skipping M_Locations entry '{location.RoomName}' (looking for '{roomtag}')", this);
#endif
                                        continue;
                                    }

                                    for (int i = 0; i < location.LocationNames.Count; i++)
                                    {
#if UNITY_EDITOR
                                        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Comparing room.name='{room.name}' vs LocationNames[{i}].RoomObjectName='{location.LocationNames[i].RoomObjectName}'", this);
#endif

                                        if (room.name == location.LocationNames[i].RoomObjectName)
                                        {
                                            // Find the actual GameObject the player must reach
                                            GameObject toSet = GameObject.Find(location.LocationNames[i].LocationObjectName);

#if UNITY_EDITOR
                                            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Matched! Looking for LocationObjectName='{location.LocationNames[i].LocationObjectName}', GameObject.Find result: {(toSet != null ? toSet.name : "NULL")}", this);
#endif

                                            locationTask.possibleAreas.Add(toSet.transform);
                                            string locationPath = Helper.GetGameObjectPath(toSet);
                                            locationAssignments.Add((locationTask.taskName, locationPath));

#if UNITY_EDITOR
                                            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Assigned location '{locationPath}' to task '{locationTask.taskName}'", this);
#endif

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
#if UNITY_EDITOR
                        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Skipping task '{locationTask.taskName}' — possibleAreas already populated ({locationTask.possibleAreas.Count} entries)", this);
#endif
                    }
                }
            }
        }

        // Send all location assignments to clients in a single RPC call
        if (locationAssignments.Count > 0)
        {
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Sending {locationAssignments.Count} location assignment(s) to clients via RPC", this);
#endif

            NetString[] taskNames = locationAssignments.Select(x => (NetString)x.taskName).ToArray();
            NetString[] locationPaths = locationAssignments.Select(x => (NetString)x.locationPath).ToArray();
            AssignObjectiveTransformsClientRpc(taskNames, locationPaths);
        }
        else
        {
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] No location assignments were made — RPC will not be sent", this);
#endif
        }
    }

    /// <summary>
    /// Scans MinigameTasks in the objective list and assigns a random Computer object to each one.
    /// If the computer type is TIMER, it also links a DoorTimer found in a Vault room.
    /// Results are synced to all clients via ClientRpc.
    /// </summary>
    private void AssignCorrectComputer()
    {
        List<NetString> computerPaths = new();
        List<NetString> taskNames = new();

        foreach (var objective in ObjectiveSystem.ObjectiveList)
        {
            if (objective.tasks == null) continue;

            foreach (var task in objective.tasks)
            {
                if (task is not MinigameTask minigameTask) continue;
                if (!minigameTask.setComputer) continue;

#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] AssignCorrectComputer: processing MinigameTask '{minigameTask.taskName}'.", this);
#endif

                List<Computer> computers = GatherComputersForTask(minigameTask);
                if (computers.Count == 0)
                {
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] No computers found for task '{minigameTask.taskName}'. Skipping.", this);
#endif
                    continue;
                }

#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Found {computers.Count} computer(s) for task '{minigameTask.taskName}'.", this);
#endif

                Computer selected = SelectAndActivateComputers(minigameTask, computers);
                if (selected == null)
                {
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] SelectAndActivateComputers returned null for task '{minigameTask.taskName}'. Skipping.", this);
#endif
                    continue;
                }

                selected.associatedTask = minigameTask;
                computerPaths.Add((NetString)Helper.GetGameObjectPath(selected.gameObject));
                taskNames.Add((NetString)minigameTask.taskName);

#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Assigned computer '{computerPaths[^1]}' to task '{minigameTask.taskName}'.", this);
#endif
            }
        }

        // Single batched RPC for all computers
        if (computerPaths.Count > 0)
        {
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Sending {computerPaths.Count} computer assignment(s) to clients via RPC.", this);
#endif
            AssignComputersClientRpc(computerPaths.ToArray(), taskNames.ToArray());
        }
        else
        {
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, "[MapManager] No computer assignments were made — RPC will not be sent.", this);
#endif
        }
    }

    /// <summary>
    /// Collects all Computer components from rooms matching the task's required room type.
    /// </summary>
    private List<Computer> GatherComputersForTask(MinigameTask task)
    {
        List<Computer> computers = new();

        var rooms = FindRoomsByTag(task.RoomType);
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] GatherComputersForTask '{task.taskName}': searching {rooms.Count} room(s) for computers.", this);
#endif

        foreach (Transform room in rooms)
            FindComputersByRoom(room, ref computers);

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] GatherComputersForTask '{task.taskName}': {computers.Count} computer(s) gathered.", this);
#endif

        return computers;
    }

    /// <summary>
    /// Handles the "random on" visual pass, then returns one selected Computer.
    /// Keeps visual state mutation separate from the selection result.
    /// </summary>
    private Computer SelectAndActivateComputers(MinigameTask task, List<Computer> computers)
    {
        if (!task.isRandomComputer)
        {
            Computer pick = computers[Random.Range(0, computers.Count)];
#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] SelectAndActivateComputers (non-random): selected '{pick.name}' for task '{task.taskName}'.", this);
#endif
            return pick;
        }

        // Shuffle a copy so we don't mutate the original list
        List<Computer> pool = new(computers);
        int activateCount = Random.Range((int)task.MinMax.x, (int)task.MinMax.y);

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] SelectAndActivateComputers (random): activating {activateCount} computer(s) from pool of {pool.Count} for task '{task.taskName}'.", this);
#endif

        Computer selected = null;

        for (int i = 0; i < activateCount && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            Computer computer = pool[index];
            pool.RemoveAt(index);

            computer.GetComponent<ComputerSettings>().IsOn.Value = true;
            selected ??= computer; // First activated computer becomes the task target

#if UNITY_EDITOR
            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Activated computer '{computer.name}' (slot {i + 1}/{activateCount}) for task '{task.taskName}'.", this);
#endif
        }

        return selected;
    }
    #endregion

    #region ClientRPCs
    /// <summary>
    /// ClientRpc that receives location assignment data and populates each LocationTask's
    /// possibleAreas list with the correct scene transforms.
    /// </summary>
    [ClientRpc]
    private void AssignObjectiveTransformsClientRpc(NetString[] taskNames, NetString[] locationPaths)
    {
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] AssignObjectiveTransformsClientRpc received {taskNames.Length} assignment(s).", this);
#endif

        for (int i = 0; i < taskNames.Length; i++)
        {
            string taskName = taskNames[i];
            string locationPath = locationPaths[i];

            GameObject locationObj = GameObject.Find(locationPath);
            if (locationObj == null)
            {
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Client RPC: could not find GameObject at path '{locationPath}' for task '{taskName}'. Skipping.", this);
#endif
                continue;
            }

            // Fall back to the singleton if the serialized reference is missing
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
#if UNITY_EDITOR
                            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Client: assigned location '{locationPath}' to LocationTask '{taskName}'.", this);
#endif
                            break;
                        }
                    }
                }
            }
            else
            {
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Client RPC: ObjectiveSystem is null, cannot assign task '{taskName}'.", this);
#endif
            }
        }
    }

    /// <summary>
    /// ClientRpc that receives computer/task assignment data from the host and applies it locally.
    /// Each client looks up the Computer by scene path and matches it to the correct MinigameTask.
    /// </summary>
    [ClientRpc]
    private void AssignComputersClientRpc(NetString[] computerPaths, NetString[] taskNames)
    {
#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] AssignComputersClientRpc received {computerPaths.Length} assignment(s).", this);
#endif

        for (int i = 0; i < computerPaths.Length; i++)
        {
            string compPath = computerPaths[i];
            string taskName = taskNames[i];

            GameObject compObj = GameObject.Find(compPath);
            if (compObj == null)
            {
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Client RPC: could not find computer GameObject at path '{compPath}' for task '{taskName}'. Skipping.", this);
#endif
                continue;
            }

            if (!compObj.TryGetComponent(out Computer computer))
            {
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Client RPC: GameObject at '{compPath}' has no Computer component. Skipping.", this);
#endif
                continue;
            }

            // Fall back to the singleton if the serialized reference is missing
            if (ObjectiveSystem == null && ObjectiveSystem.Instance != null)
                ObjectiveSystem = ObjectiveSystem.Instance;

            // Find the matching MinigameTask by name and assign it to the computer
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
#if UNITY_EDITOR
                            LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Client: assigned MinigameTask '{taskName}' to computer '{compPath}'.", this);
#endif
                            break;
                        }
                    }

                    if (computer.associatedTask != null) break;
                }
            }
            else
            {
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] Client RPC: ObjectiveSystem is null, cannot assign computer for task '{taskName}'.", this);
#endif
            }
        }
    }
    #endregion

    #region HELPERS
    private static void ShowRoom(Transform t)
    {
        if (t.TryGetComponent(out RoomVisibility vis))
            vis.Show();
    }

    /// <summary>
    /// A room with no RoomVisibility component is always-present in the scene (no script needed).
    /// A room WITH the component must have IsVisible = true to be considered active.
    /// </summary>
    private static bool IsRoomVisible(Transform t) =>
        !t.TryGetComponent(out RoomVisibility vis) || vis.IsVisible.Value;

    private static bool IsRoomType(Transform t, RoomTypeTag tag) =>
        t.TryGetComponent<RoomType>(out RoomType rt) && rt.Tag == tag;

    private static RoomTypeTag GetRoomType(Transform t) =>
        t.TryGetComponent<RoomType>(out RoomType rt) ? rt.Tag : null;

    /// <summary>
    /// Attempts to derive a room type name from a task's display name.
    /// Does a case-insensitive substring match against all currently activated room type names.
    /// E.g. task "Hack the vault computer" might match the activated room type "Vault".
    /// </summary>
    private string ExtractRoomTagFromTaskName(string taskName)
    {
        string lowerTaskName = taskName.ToLower();

        foreach (var roomData in _activatedRooms)
        {
            if (lowerTaskName.Contains(roomData.roomType.name.ToLower()))
                return roomData.roomType.name;
        }

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] ExtractRoomTagFromTaskName: no matching room type found in task name '{taskName}'.", this);
#endif
        return null;
    }

    private List<Transform> FindRoomsByTag(string tag)
    {
        List<Transform> result = new();

        foreach (var r in _activatedRooms)
        {
            if (!r.roomType.name.Equals(tag, System.StringComparison.OrdinalIgnoreCase))
                continue;

            GameObject area = GameObject.Find(r.areaName);
            if (area == null)
            {
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] FindRoomsByTag: area GameObject '{r.areaName}' not found in scene.", this);
#endif
                continue;
            }

            foreach (Transform child in area.transform)
                if (IsRoomType(child, r.roomType) && IsRoomVisible(child))
                    result.Add(child);
        }

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, $"[MapManager] FindRoomsByTag('{tag}'): {result.Count} visible room(s) found.", this);
#endif
        return result;
    }

    /// <summary>
    /// Recursively searches a room's hierarchy for Computer components and adds them to the list.
    /// </summary>
    private void FindComputersByRoom(Transform room, ref List<Computer> computers)
    {
        foreach (Transform child in room)
        {
            if (child.TryGetComponent(out Computer computer))
                computers.Add(computer);

            FindComputersByRoom(child, ref computers); // Recurse into nested children
        }
    }
    #endregion
}