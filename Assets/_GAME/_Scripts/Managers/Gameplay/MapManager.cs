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

    #region Inspector Fields
    [Header("Map Settings")]
    public M_Settings Map;
    public M_Areas[] Areas;
    public RandomObjectiveData[] RandomObjectives;
    public ObjectiveHintData[] ObjectiveHints;
    public M_RandomDialouge MapRandomDialouge;
    public ObjectiveSystem ObjectiveSystem;

    [SerializeField] private UnityEvent OnMapGenerated; // Optional event that fires after map generation is complete, for hooking up custom logic in the inspector

    [Header("Room Tags")]
    [SerializeField] private RoomTypeTag _hallTag;

    [Header("NavMesh")]
    [SerializeField] private NavMeshSurface _navMeshSurface;
    #endregion

    #region State
    public event System.Action OnNavMeshReady;

    private Dictionary<RoomTypeTag, (int min, int max)> _roomLimits = new();
    private Dictionary<RoomTypeTag, int> _currentCount = new();
    private List<(string areaName, RoomTypeTag roomType)> _activatedRooms = new();

    public NetworkList<NetRoomData> syncedRooms;
    #endregion

    #region Unity Lifecycle
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

        if (IsServer)
        {
            OnNavMeshReady += OnNavMeshBaked;

            // Generate rooms
            AllocateRooms();

            // Send results to clients
            foreach (var (area, room) in _activatedRooms)
            {
                syncedRooms.Add(new NetRoomData(area, room.name));
            }
            // Bake NavMesh AFTER rooms are spawned
            StartCoroutine(BakeNavMeshThenInit());
        }
    }
    #endregion

    #region Map Generation (Server Only)
    /// <summary>
    /// Core room allocation logic (host only).
    /// Iterates over every room type, picks random areas, respects min/max limits,
    /// processes dependencies, then fills remaining areas with hallways.
    /// </summary>
    private void AllocateRooms()
    {
        M_Rooms rooms = Map.MapRooms;
        List<M_Areas> availableAreas = new List<M_Areas>(Areas);
        SetRoomLimits(rooms);

        // --- Pass 1: Place all non-hall room types ---
        foreach (var kvp in _roomLimits)
        {
            RoomTypeTag roomTag = kvp.Key;
            if (roomTag == _hallTag) continue; // Halls are handled separately

            int min = kvp.Value.min;
            int max = kvp.Value.max;
            // If there's a minimum, pick a random count between min and max; otherwise skip this type
            int roomCount = min > 0 ? Random.Range(min, max + 1) : 0;

            for (int i = 0; i < roomCount; i++)
            {
                int areaRef = -1;
                GameObject room = PickRandomArea(roomTag, availableAreas, ref areaRef);
                if (room == null) continue; // No valid area found after max attempts

                RoomTypeTag roomType = GetRoomType(room.transform);
                // Skip if this room type has already hit its cap
                if (roomType == null || _currentCount[roomType] >= _roomLimits[roomType].max) continue;

                _currentCount[roomType]++;
                availableAreas.RemoveAt(areaRef); // Mark this area as used
                room.SetActive(true);

                string parentAreaName = room.transform.parent.name;
                _activatedRooms.Add((parentAreaName, roomType));

                // Check and process any dependencies this room may have
                ProcessDependencies(parentAreaName, roomType, availableAreas);
            }
        }

        // --- Pass 2: Fill any areas that couldn't be halls but got no room ---
        CheckNonHallAreas(availableAreas);

        // --- Pass 3: Fill remaining areas with hallways ---
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
                    break; // Found the hall gameobject
                }
            }
        }

#if UNITY_EDITOR
        // Debug output — logs every activated room and its type
        string s = "";
        s += "--- Activated Roooms ---\n";
        foreach (var kvp in _activatedRooms)
        {
            s += kvp.areaName + ": " + kvp.roomType.name + "\n";
        }
        LoggerEvent.Log(LogPrefix.Environment, s, this);
#endif
    }

    /// <summary>
    /// Reads the room type limits from the map settings and populates the working dictionaries.
    /// A max of 0 in the settings is treated as unlimited (99).
    /// </summary>
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

    /// <summary>
    /// Randomly picks an available area that contains a room matching the requested type.
    /// Tries up to 20 times before giving up to avoid infinite loops.
    /// </summary>
    /// <param name="selectedRoom">The room type we're looking for.</param>
    /// <param name="availableAreas">The pool of areas not yet assigned a room.</param>
    /// <param name="areaIndex">Output: the index in availableAreas of the chosen area.</param>
    /// <returns>The matching child GameObject, or null if none found.</returns>
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

#if UNITY_EDITOR
        LoggerEvent.LogWarning(LogPrefix.Environment, $"Failed to find area for room type '{selectedRoom.name}' after {maxAttempts} attempts.", this);
#endif

        return null;
    }

    /// <summary>
    /// After placing a room, checks whether that placement triggers any dependency rules.
    /// For example: placing a "Vault" room may require a specific "SecurityDesk" room to also exist.
    /// If the target area already has the wrong room type, it's swapped out.
    /// If the required room type is already at its cap, an existing one is freed first.
    /// </summary>
    private void ProcessDependencies(string areaName, RoomTypeTag roomType, List<M_Areas> availableAreas)
    {
        // --- Step 1: Look up the config for the area that was just assigned a room ---
        M_Areas areaConfig = Areas.FirstOrDefault(a => a.Area == areaName);

        if (areaConfig.Dependencies == null || areaConfig.Dependencies.Length == 0)
            return;

        foreach (M_Dependency dependency in areaConfig.Dependencies)
        {
            // Only act if this dependency is triggered by the room type we just placed
            if (dependency.TriggerRoomType != roomType)
                continue;

            GameObject targetArea = GameObject.Find(dependency.TargetAreaName);
            GameObject existingRoom = null;
            RoomTypeTag existingRoomType = null;

            // --- Step 2: Check what room (if any) is already active in the target area ---
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

            // --- Step 3: If the target area already has the correct room type, nothing to do ---
            if (existingRoom != null && existingRoomType == dependency.RequiredRoomType)
                continue;

            // --- Step 4: Remove whatever wrong room was in the target area ---
            if (existingRoom != null)
            {
                existingRoom.SetActive(false);
                _currentCount[existingRoomType]--;
                _activatedRooms.Remove((dependency.TargetAreaName, existingRoomType));
            }

            // --- Step 5: If the required room type is at its global cap, free up one instance elsewhere ---
            if (_currentCount[dependency.RequiredRoomType] >= _roomLimits[dependency.RequiredRoomType].max)
            {
#if UNITY_EDITOR
                LoggerEvent.LogWarning(LogPrefix.Environment, $"Room type '{dependency.RequiredRoomType.name}' is at its cap. Attempting to free one up for dependency.", this);
#endif

                // Prefer freeing a room in the exact target area; fall back to any area with that type
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

                        // The freed area is now available again for future allocation
                        M_Areas oldArea = Areas.FirstOrDefault(a => a.Area == oldEntry.areaName);
                        if (oldArea != null && !availableAreas.Contains(oldArea))
                            availableAreas.Add(oldArea);

                        break;
                    }
                }
            }

            // --- Step 6: Activate the required room type in the target area ---
            for (int i = 0; i < targetArea.transform.childCount; i++)
            {
                Transform child = targetArea.transform.GetChild(i);
                if (IsRoomType(child, dependency.RequiredRoomType))
                {
                    child.gameObject.SetActive(true);
                    _currentCount[dependency.RequiredRoomType]++;
                    _activatedRooms.Add((dependency.TargetAreaName, dependency.RequiredRoomType));

                    // Remove the target area from the available pool so it won't be overwritten
                    M_Areas trgArea = availableAreas.FirstOrDefault(a => a.Area == dependency.TargetAreaName);
                    if (trgArea != null)
                        availableAreas.Remove(trgArea);
                }
            }
        }
    }

    /// <summary>
    /// Ensures every area marked as "cannot be a hall" has at least one active room.
    /// If an area is still empty, a random valid room type from that area's allowed list is placed.
    /// </summary>
    private void CheckNonHallAreas(List<M_Areas> availableAreas)
    {
        var emptyNonHallAreas = availableAreas.Where(a => !a.CanBeHall).ToList();

        foreach (M_Areas area in emptyNonHallAreas)
        {
            GameObject areaObj = GameObject.Find(area.Area);
            bool hasActiveRoom = false;

            // Check if any child is already active
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
                // Pick a random room type from this area's allowed types
                RoomTypeTag selectedRoomType = area.Rooms[Random.Range(0, area.Rooms.Length)];

                for (int i = 0; i < areaObj.transform.childCount; i++)
                {
                    Transform child = areaObj.transform.GetChild(i);
                    if (IsRoomType(child, selectedRoomType))
                    {
                        // Only place if we haven't exceeded this room type's global cap
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

    #region NavMesh
    /// <summary>
    /// Coroutine that asynchronously rebuilds the NavMesh using the current scene geometry,
    /// then fires the OnNavMeshReady event once complete.
    /// </summary>
    private IEnumerator BakeNavMeshThenInit()
    {
        AsyncOperation bake = _navMeshSurface.UpdateNavMesh(_navMeshSurface.navMeshData);
        yield return bake; // Wait until the bake finishes before continuing

#if UNITY_EDITOR
        LoggerEvent.Log(LogPrefix.Environment, "NavMesh baking complete.", this);
#endif

        OnNavMeshReady?.Invoke();
        OnMapGenerated?.Invoke();
    }

    /// <summary>
    /// Called on the server once the NavMesh has finished baking.
    /// Calculates max loot payout, syncs rooms to clients, and sets up objectives.
    /// </summary>
    private void OnNavMeshBaked()
    {
        // Sum the total of all active loot value
        int max = 0;
        foreach (Loot loot in GameObject.FindObjectsByType<Loot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            max += loot.LootValue;
        }
        // Share with clients
        NetStore.Instance.SetMaxPayoutServerRpc(max);

        // Tell clients which rooms to enable
        SyncRoomsClientRpc();

        // Assign world-space transforms and computers to objective tasks
        AssignObjectiveTransforms();
        AssignCorrectComputer();
        SpawnRandomObjectives();
        SpawnObjectiveHints();
    }
    #endregion

    #region Objective Assignment (Server Only)
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
                if(!minigameTask.setComputer) continue;

                List<Computer> computers = GatherComputersForTask(minigameTask);
                if (computers.Count == 0) continue;

                Computer selected = SelectAndActivateComputers(minigameTask, computers);
                if (selected == null) continue;

                selected.associatedTask = minigameTask;
                computerPaths.Add((NetString)Helper.GetGameObjectPath(selected.gameObject));
                taskNames.Add((NetString)minigameTask.taskName);

#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Environment,
                    $"Assigned computer '{computerPaths[^1]}' to task '{minigameTask.taskName}'.", this);
#endif
            }
        }

        // Single batched RPC for all computers
        if (computerPaths.Count > 0)
            AssignComputersClientRpc(computerPaths.ToArray(), taskNames.ToArray());
    }

    /// <summary>
    /// Collects all Computer components from rooms matching the task's required room type.
    /// </summary>
    private List<Computer> GatherComputersForTask(MinigameTask task)
    {
        List<Computer> computers = new();

        foreach (Transform room in FindRoomsByTag(task.RoomType))
            FindComputersByRoom(room, ref computers);

        return computers;
    }

    /// <summary>
    /// Handles the "random on" visual pass, then returns one selected Computer.
    /// Keeps visual state mutation separate from the selection result.
    /// </summary>
    private Computer SelectAndActivateComputers(MinigameTask task, List<Computer> computers)
    {
        if (!task.isRandomComputer)
            return computers[Random.Range(0, computers.Count)];

        // Shuffle a copy so we don't mutate the original list
        List<Computer> pool = new(computers);
        int activateCount = Random.Range((int)task.MinMax.x, (int)task.MinMax.y);

        Computer selected = null;

        for (int i = 0; i < activateCount && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            Computer computer = pool[index];
            pool.RemoveAt(index);

            computer.GetComponent<ComputerSettings>().SetIsOnRpc(true);
            selected ??= computer; // First activated computer becomes the task target
        }

        return selected;
    }

    private void SpawnRandomObjectives()
    {
        foreach (RandomObjectiveData data in RandomObjectives)
        {
            List<NetString> locationPaths = new();

            List<Transform> candidateRooms = FindRoomsByTag(data.RequiredRoomType.name);

            foreach (Transform room in candidateRooms)
            {
                List<GameObject> items = new();
                Helper.FindItemsByRoom(room, data.SpawnItemType, ref items);

                int remaining = data.GetRandomSpawnCount();

                while (remaining > 0 && items.Count > 0)
                {
                    int index = Random.Range(0, items.Count);
                    GameObject chosen = items[index];
                    items.RemoveAt(index);

                    chosen.SetActive(true);
                    locationPaths.Add((NetString)Helper.GetGameObjectPath(chosen));
                    remaining--;
                }
            }

            if (locationPaths.Count > 0)
                SpawnRandomObjectivesClientRpc(locationPaths.ToArray());
        }
    }

    private void SpawnObjectiveHints()
    {
        foreach (ObjectiveHintData data in ObjectiveHints)
        {
            Transform targetRoom = Helper.GoToTaskRoom(data.Index.x, data.Index.y);

            if (targetRoom == null)
            {
#if UNITY_EDITOR
                LoggerEvent.LogWarning(LogPrefix.Environment, $"Couldn't find room for Objective and Task: {data.Index}. Continuing", this);
#endif
                continue;
            }

            List<GameObject> items = new();
            Helper.FindItemsByRoom(targetRoom, data.SpawnItemType, ref items);

            foreach (GameObject item in items)
            {
                if (item.TryGetComponent(out RandomObject objectData))
                {
                    objectData.ChangeStateRpc(true);
                }
                else
                {
#if UNITY_EDITOR
                    LoggerEvent.Log(LogPrefix.Environment, $"{item.name} has no ObjectData. Continuing", this);
#endif
                    continue;
                }
            }
        }
    }
    #endregion

    #region Client RPCs
    /// <summary>
    /// Tells non-host clients which rooms to activate, using the NetworkList that the host populated.
    /// The host skips this because it already activated its rooms during AllocateRooms().
    /// </summary>
    [ClientRpc]
    private void SyncRoomsClientRpc()
    {
        if (IsHost) return;

        foreach (var room in syncedRooms)
        {
            GameObject areaObj = GameObject.Find(room.AreaName);
            if (areaObj == null) continue;

            // Find the child that matches both the area and the room type name, then enable it
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

    /// <summary>
    /// ClientRpc that receives location assignment data and populates each LocationTask's
    /// possibleAreas list with the correct scene transforms.
    /// </summary>
    [ClientRpc]
    private void AssignObjectiveTransformsClientRpc(NetString[] taskNames, NetString[] locationPaths)
    {
        for (int i = 0; i < taskNames.Length; i++)
        {
            string taskName = taskNames[i];
            string locationPath = locationPaths[i];

            GameObject locationObj = GameObject.Find(locationPath);
            if (locationObj == null) continue;

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
                            break;
                        }
                    }
                }
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
        for (int i = 0; i < computerPaths.Length; i++)
        {
            string compPath = computerPaths[i];
            string taskName = taskNames[i];

            GameObject compObj = GameObject.Find(compPath);
            if (compObj == null) continue;

            if (!compObj.TryGetComponent(out Computer computer)) continue;

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
                            break;
                        }
                    }

                    if (computer.associatedTask != null) break;
                }
            }
        }
    }

    [Rpc(SendTo.NotServer)]
    public void SpawnRandomObjectivesClientRpc(NetString[] itemsPaths)
    {
        foreach (NetString path in itemsPaths)
        {
            GameObject itemObj = GameObject.Find(path);
            if (itemObj != null)
                itemObj.SetActive(true);
        }
    }

    #endregion

    #region Helpers
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

        return null;
    }

    /// <summary>
    /// Finds all currently active room transforms whose type tag matches the given name.
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
    /// <summary>
    /// Searches the immediate children of a room transform for a DoorTimer component.
    /// Returns the first one found, or null.
    /// </summary>
    private DoorTimer FindTimerByRoom(Transform room)
    {
        foreach (Transform child in room)
        {
            if (child.TryGetComponent(out DoorTimer timer))
                return timer;
        }

        return null;
    }
    /// <summary>
    /// Returns true if the given Transform has a RoomType component whose Tag matches the provided tag.
    /// </summary>
    private static bool IsRoomType(Transform t, RoomTypeTag tag) =>
        t.TryGetComponent<RoomType>(out RoomType rt) && rt.Tag == tag;

    /// <summary>
    /// Returns the RoomTypeTag from a Transform's RoomType component, or null if it doesn't have one.
    /// </summary>
    private static RoomTypeTag GetRoomType(Transform t) =>
        t.TryGetComponent<RoomType>(out RoomType rt) ? rt.Tag : null;
    #endregion

}