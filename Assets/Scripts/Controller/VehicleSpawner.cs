using SnowPlow.Controller.NPCMovement;
using SnowPlow.Controller.Traffic;
using SnowPlow.Model.Map;
using SnowPlow.Model.Map.Generator;
using SnowPlow.Model.Players;
using SnowPlow.Model.Tools;
using SnowPlow.Model.Vehicles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using SnowPlowVehicle = SnowPlow.Model.Vehicles.SnowPlow;

namespace SnowPlow.Controller.Spawning
{
    // ez a class felel a jarmuvek spawnolasert
    // nem pathfindingol, nem mozgat, nem valaszt celt
    // csak letrehozza a modelt + a prefab GameObjectet, es osszekoti oket
    public class VehicleSpawner : NetworkBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject carNpcPrefab;
        [SerializeField] private GameObject snowPlowNpcPrefab;
        [SerializeField] private GameObject playerSnowPlowPrefab;
        [SerializeField] private GameObject busPrefab;

        [Header("Traffic")]
        [SerializeField] private VehicleOccupancyManager occupancyManager;

        [Header("Initial Spawn")]
        [SerializeField] private int initialCarCount = 6;
        [SerializeField] private bool spawnPlayerSnowPlowOnStart = true;
        [SerializeField] private bool spawnPlayerBusOnStart = false;

        [Header("Location Markers")]
        [SerializeField] private GameObject[] homePrefabs; // Change from homeMarkerPrefab
        [SerializeField] private GameObject[] workPrefabs; // Change from workMarkerPrefab

        private readonly System.Random rng = new();

        private MapData mapData;
        private MapVisualizer mapVisualizer;
        private bool isInitialized;

        // ezt a map letrehozasa utan kell meghivni
        // fontos: a MapVisualizer.Visualize(mapData) mar fusson le elotte,
        // mert csak akkor lesz feltoltve a SegmentDirectory
        public void Initialize(MapData data, MapVisualizer visualizer)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (visualizer == null) throw new ArgumentNullException(nameof(visualizer));
            if (carNpcPrefab == null) throw new InvalidOperationException("Car NPC prefab is missing.");
            if (snowPlowNpcPrefab == null) throw new InvalidOperationException("SnowPlow prefab is missing.");
            if (occupancyManager == null) throw new InvalidOperationException("VehicleOccupancyManager is missing.");
            if (spawnPlayerSnowPlowOnStart && playerSnowPlowPrefab == null) throw new InvalidOperationException("Player SnowPlow prefab is missing.");

            // --- CLEAR OLD HOUSE POSITIONS HERE ---
            HouseSpawner.ClearRegistry();

            mapData = data;
            mapVisualizer = visualizer;
            isInitialized = true;
            //SpawnInitialVehicles();
            if (IsServer)
            {
                SpawnInitialVehicles();
            }
            //SpawnPlayerSnowPlow(NetworkManager.Singleton.LocalClientId);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            //SpawnPlayerSnowPlow(NetworkManager.ServerClientId);
            StartCoroutine(WaitForInitialization());
        }
        private System.Collections.IEnumerator WaitForInitialization()
        {
            while (!isInitialized)
            {
                yield return null;
            }
            while (mapVisualizer == null ||
          mapVisualizer.SegmentDirectory == null ||
          mapVisualizer.SegmentDirectory.Count == 0)
            {
                yield return null;
            }

            yield return null;
            yield return null;
            Debug.Log("VehicleSpawner initialized, spawning players...");

            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                SpawnPlayerVehicle(clientId);
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            // Host already spawned in WaitForInitialization
            if (clientId == NetworkManager.ServerClientId)
                return;

            StartCoroutine(SpawnClientWhenReady(clientId));
        }
        private System.Collections.IEnumerator SpawnClientWhenReady(ulong clientId)
        {
            while (!isInitialized)
            {
                yield return null;
            }
            while (mapVisualizer == null ||
           mapVisualizer.SegmentDirectory == null ||
           mapVisualizer.SegmentDirectory.Count == 0)
            {
                yield return null;
            }
            yield return null;
            yield return null;

            Debug.Log("[SPAWNER] Map fully ready, spawning player vehicle");
            SpawnPlayerVehicle(clientId);
        }

        // jatek eleji spawn
        // most: 6 auto + 1 jatekos hokotro
        // NPC hokotro alapbol NEM spawnol, azt majd shop hivja
        private void SpawnInitialVehicles()
        {
            //csak a host
            if (!IsServer)
                return;

            for (int i = 0; i < initialCarCount; i++)
            {
                SpawnCarNPC();
            }

            if (spawnPlayerBusOnStart)
            {
                bool hasHumanBusDriver =
                GameManager.Instance.Players.Any(p => p.Role == PlayerRole.BusDriver);

                if (!hasHumanBusDriver)
                {
                    SpawnBus();
                }
            }
        }

        public Car SpawnCarNPC()
        {
            EnsureInitialized();

            Car car = new();
            LanePosition startPosition = GetRandomFreePosition();

            car.CurrentPosition = startPosition;

            GameObject instance = InstantiateVehiclePrefab(carNpcPrefab, startPosition, "NPC_Car");

            NPCVehicleMover mover = instance.GetComponent<NPCVehicleMover>();
            NPCVehicleBehaviour behaviour = instance.GetComponent<NPCVehicleBehaviour>();

            if (mover == null) throw new InvalidOperationException("NPCVehicleMover is missing from car prefab.");
            if (behaviour == null) throw new InvalidOperationException("NPCVehicleBehaviour is missing from car prefab.");

            mover.SetMapVisualizer(mapVisualizer);

            VehicleSegmentSensor sensor = instance.GetComponent<VehicleSegmentSensor>();

            if (sensor == null)
            {
                sensor = instance.AddComponent<VehicleSegmentSensor>();
            }

            sensor.Initialize(car);

            behaviour.SetMapVisualizer(mapVisualizer);
            behaviour.Initialize(car, mapData);

            GameObject selectedHomePrefab = null;
            if (homePrefabs != null && homePrefabs.Length > 0)
            {
                selectedHomePrefab = homePrefabs[rng.Next(homePrefabs.Length)];
            }

            GameObject selectedWorkPrefab = null;
            if (workPrefabs != null && workPrefabs.Length > 0)
            {
                selectedWorkPrefab = workPrefabs[rng.Next(workPrefabs.Length)];
            }

            var markerSpawner = instance.AddComponent<HouseSpawner>();
            markerSpawner.Initialize(
                car,
                mapVisualizer,
                selectedHomePrefab,
                selectedWorkPrefab,
                vehicleZOffset - 0.5f
            );

            return car;
        }

        // ezt majd a shop hivja, amikor veszunk egy NPC hokotrot

        public SnowPlowVehicle SpawnSnowPlowNPC(IPlowTool tool)
        {
            if (!IsServer)
            {
                Debug.LogError(
                    "SpawnSnowPlowNPC CALLED ON CLIENT!"
                );

                return null;
            }
            EnsureInitialized();

            if (tool == null)
                throw new ArgumentNullException(nameof(tool));

            SnowPlowVehicle snowPlow = new(tool);
            LanePosition startPosition = GetRandomFreePosition();

            snowPlow.CurrentPosition = startPosition;

            GameObject instance = InstantiateVehiclePrefab(snowPlowNpcPrefab, startPosition, "NPC_SnowPlow");

            NPCVehicleMover mover = instance.GetComponent<NPCVehicleMover>();
            NPCVehicleBehaviour behaviour = instance.GetComponent<NPCVehicleBehaviour>();

            if (mover == null) throw new InvalidOperationException("NPCVehicleMover is missing from snowplow NPC prefab.");
            if (behaviour == null) throw new InvalidOperationException("NPCVehicleBehaviour is missing from snowplow NPC prefab.");

            mover.SetMapVisualizer(mapVisualizer);

            VehicleSegmentSensor sensor = instance.GetComponent<VehicleSegmentSensor>();
            if (sensor == null)
            {
                sensor = instance.AddComponent<VehicleSegmentSensor>();
            }

            sensor.Initialize(snowPlow);

            if (global::GameManager.Instance != null && global::GameManager.Instance.CurrentPlayer != null)
            {
                global::GameManager.Instance.CurrentPlayer.AddVehicle(snowPlow);
            }

            behaviour.Initialize(snowPlow, mapData);

            NPCPlowVisuals npcVisuals = instance.GetComponent<NPCPlowVisuals>();
            if (npcVisuals != null)
            {
                npcVisuals.SetPlowModel(snowPlow);
            }

            return snowPlow;
        }

        public SnowPlowVehicle SpawnPlayerSnowPlow(ulong clientId)
        {
            EnsureInitialized();

            Player player =
                GameManager.Instance.Players.Find(
                    p => p.OwnerClientId == clientId
                );

            if (player == null)
            {
                Debug.LogError($"No player found for client {clientId}");
                return null;
            }

            SnowPlowVehicle playerSnowPlow =
                player.GetOwnedSnowPlow();
            

            if (playerSnowPlow == null)
            {
                Debug.LogError($"Player {player.Name} has no SnowPlow!");
                return null;
            }
            playerSnowPlow.Owner = player;
            Debug.Log(
    $"[SPAWNER] Assigned owner {player.Name} to plow. Team: {player.Team?.Name}"
);
            LanePosition startPosition = GetRandomFreePosition();

            playerSnowPlow.CurrentPosition = startPosition;

            GameObject instance = InstantiateVehiclePrefab(
                playerSnowPlowPrefab,
                startPosition,
                "PlayerCar",
                clientId
            );

            NPCVehicleBehaviour npcBehaviour =
                instance.GetComponent<NPCVehicleBehaviour>();

            if (npcBehaviour != null)
            {
                npcBehaviour.enabled = false;
            }

            NPCVehicleMover npcMover =
                instance.GetComponent<NPCVehicleMover>();

            if (npcMover != null)
            {
                npcMover.enabled = false;
            }

            VehicleSegmentSensor sensor =
                instance.GetComponent<VehicleSegmentSensor>();

            if (sensor == null)
            {
                sensor = instance.AddComponent<VehicleSegmentSensor>();
            }

            sensor.Initialize(playerSnowPlow);

            occupancyManager.RegisterVehicle(
                playerSnowPlow,
                startPosition
            );

            PlowMovement plowMovement = instance.GetComponent<PlowMovement>();

            if (plowMovement != null)
            {
                plowMovement.OwnerClientId.Value =
                    player.OwnerClientId;

                plowMovement.SetPlowModel(playerSnowPlow);
            }

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                CameraFollow cameraFollow =
                    Camera.main.GetComponent<CameraFollow>();

                if (cameraFollow != null)
                {
                    cameraFollow.SetTarget(instance.transform);
                }
            }

            return playerSnowPlow;
            
        }



        public Bus SpawnBus()
        {
            EnsureInitialized();

            if (busPrefab == null) throw new InvalidOperationException("Bus prefab is missing.");

            Bus bus = new();
            LanePosition startPosition = GetRandomFreePosition();
            bus.CurrentPosition = startPosition;

            // Pick two stations far from each other
            (LaneSegment stationA, LaneSegment stationB) = GetTwoFarStations();
            bus.StationA = stationA;
            bus.StationB = stationB;

            Vector3 posA = mapVisualizer.SegmentDirectory[stationA].transform.position;
            Vector3 posB = mapVisualizer.SegmentDirectory[stationB].transform.position;

            string nameA = mapVisualizer.SegmentDirectory[stationA].gameObject.name;
            string nameB = mapVisualizer.SegmentDirectory[stationB].gameObject.name;

            Debug.Log($"[Bus] Station A: {nameA} at {posA}");
            Debug.Log($"[Bus] Station B: {nameB} at {posB}");
            Debug.Log($"[Bus] Distance between stations: {Vector3.Distance(posA, posB):F1} units");

            MarkStationWithNeighbors(stationA);
            MarkStationWithNeighbors(stationB);
            SpawnStationsClientRpc(
    mapVisualizer.SegmentDirectory[stationA].gameObject.name,
    mapVisualizer.SegmentDirectory[stationB].gameObject.name
);


            GameObject instance = InstantiateVehiclePrefab(busPrefab, startPosition, "Bus");

            NPCVehicleBehaviour npcBehaviour = instance.GetComponent<NPCVehicleBehaviour>();
            if (npcBehaviour != null) npcBehaviour.enabled = false;

            NPCVehicleMover npcMover = instance.GetComponent<NPCVehicleMover>();
            if (npcMover != null) npcMover.enabled = false;

            VehicleSegmentSensor sensor = instance.GetComponent<VehicleSegmentSensor>();
            if (sensor == null) sensor = instance.AddComponent<VehicleSegmentSensor>();
            sensor.Initialize(bus);

            occupancyManager.RegisterVehicle(bus, startPosition);

            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
            if (cameraFollow != null) cameraFollow.SetTarget(instance.transform);

            BusMovement busMovement = instance.GetComponent<BusMovement>();
            if (busMovement != null)
            {
                busMovement.SetStations(
                    mapVisualizer.SegmentDirectory[stationA],
                    mapVisualizer.SegmentDirectory[stationB]
                );
                busMovement.SetBusModel(bus);
            }

            StationArrowIndicator arrowIndicator = instance.GetComponent<StationArrowIndicator>();
            if (arrowIndicator != null)
            {
                arrowIndicator.SetStations(
                    mapVisualizer.SegmentDirectory[stationA],
                    mapVisualizer.SegmentDirectory[stationB]
                );
            }

            if (global::GameManager.Instance != null &&
                global::GameManager.Instance.CurrentPlayer != null)
            {
                global::GameManager.Instance.CurrentPlayer.AddVehicle(bus);
            }

            return bus;
        }

        private void MarkStationWithNeighbors(LaneSegment stationSegment)
        {
            Debug.Log("MarkStationWithNeighbors CALLED");
            foreach (Road road in mapData.Roads)
            {
                foreach (Lane lane in road.LanesTowardsA)
                {
                    MarkLaneStationVisuals(lane, stationSegment);
                }

                foreach (Lane lane in road.LanesTowardsB)
                {
                    MarkLaneStationVisuals(lane, stationSegment);
                }
            }
        }

        private void MarkLaneStationVisuals(Lane lane, LaneSegment stationSegment)
        {
            int index = -1;

            for (int i = 0; i < lane.Segments.Count; i++)
            {
                if (lane.Segments[i] == stationSegment)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
                return;

            for (int offset = -1; offset <= 1; offset++)
            {
                int targetIndex = index + offset;

                if (targetIndex < 0 || targetIndex >= lane.Segments.Count)
                    continue;

                LaneSegment segment = lane.Segments[targetIndex];

                if (!mapVisualizer.SegmentDirectory.TryGetValue(segment, out VisualSegment visual))
                    continue;

                if (offset == 0)
                {
                    // Middle segment gets sign + lines
                    visual.MarkAsStation();
                }
                else
                {
                    // Neighbor segments get only lines
                    visual.MarkAsStationLine();
                }
            }
        }

        private GameObject InstantiateVehiclePrefab(
            GameObject prefab,
            LanePosition startPosition,
            string objectName,
            ulong? ownerClientId = null
        )
        {
            Vector3 worldPosition = GetWorldPosition(startPosition);

            GameObject instance = Instantiate(
                prefab,
                worldPosition,
                Quaternion.identity,
                transform
            );

            instance.name = objectName;

            //Networkon is spawnoljon, ne csak local
            NetworkObject networkObject = instance.GetComponent<NetworkObject>();

            if (networkObject != null)
            {
                if (ownerClientId.HasValue)
                {
                    networkObject.SpawnWithOwnership(ownerClientId.Value);
                }
                else
                {
                    networkObject.Spawn();
                }
            }
            else
            {
                Debug.LogError($"{objectName} prefab missing NetworkObject!");
            }

            return instance;
        }

        private LanePosition GetRandomFreePosition()
        {
            List<LanePosition> allPositions = CollectAllLanePositions();

            if (allPositions.Count == 0)
            {
                throw new InvalidOperationException("There are no lane positions in the map.");
            }

            for (int attempt = 0; attempt < 100; attempt++)
            {
                LanePosition candidate = allPositions[rng.Next(allPositions.Count)];

                if (!occupancyManager.IsOccupied(candidate))
                {
                    return candidate;
                }
            }

            return allPositions[rng.Next(allPositions.Count)];
        }

        private List<LanePosition> CollectAllLanePositions()
        {
            List<LanePosition> result = new();

            foreach (Road road in mapData.Roads)
            {
                foreach (Lane lane in road.LanesTowardsA)
                {
                    AddLanePositions(lane, result);
                }

                foreach (Lane lane in road.LanesTowardsB)
                {
                    AddLanePositions(lane, result);
                }
            }

            return result;
        }

        private void AddLanePositions(Lane lane, List<LanePosition> result)
        {
            for (int i = 0; i < lane.Segments.Count; i++)
            {
                result.Add(new LanePosition(lane, i));
            }
        }

        [SerializeField] private float vehicleZOffset = -1f;

        private Vector3 GetWorldPosition(LanePosition position)
        {
            LaneSegment segment = position.Lane[position.SegmentIndex];

            if (!mapVisualizer.SegmentDirectory.TryGetValue(segment, out VisualSegment visualSegment))
            {
                throw new InvalidOperationException("VisualSegment was not found for the selected LanePosition.");
            }

            Vector3 pos = visualSegment.transform.position;
            pos.z = vehicleZOffset;
            return pos;
        }

        private void EnsureInitialized()
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("VehicleSpawner is not initialized yet.");
            }
        }

        // Picks the two segments with the greatest world-space distance between them
        /*private (LaneSegment, LaneSegment) GetTwoFarStations()
        {
            // Group segments by their lane prefix (e.g. "Segment_3_Lane2")
            // to determine which is the first and last per lane
            var laneGroups = new Dictionary<string, List<(int index, LaneSegment seg)>>();

            foreach (var kvp in mapVisualizer.SegmentDirectory)
            {
                string name = kvp.Value.gameObject.name;

                // Name format: Segment_{roadId}_Lane{laneId}_S{index}
                int splitAt = name.LastIndexOf("_S");
                if (splitAt < 0) continue;

                string prefix = name.Substring(0, splitAt);
                string indexPart = name.Substring(splitAt + 2);

                if (!int.TryParse(indexPart, out int index)) continue;

                if (!laneGroups.ContainsKey(prefix))
                    laneGroups[prefix] = new List<(int, LaneSegment)>();

                laneGroups[prefix].Add((index, kvp.Key));
            }

            // Collect only middle segments (not first or last of their lane)
            List<LaneSegment> validSegments = new List<LaneSegment>();

            foreach (var group in laneGroups.Values)
            {
                int maxIndex = 0;
                foreach (var (index, _) in group)
                    if (index > maxIndex) maxIndex = index;

                foreach (var (index, seg) in group)
                {
                    if (index >= 6 && index <= maxIndex - 6)
                        validSegments.Add(seg);
                }
            }

            if (validSegments.Count < 2)
                throw new InvalidOperationException("Not enough valid segments to place two stations.");

            LaneSegment bestA = null;
            LaneSegment bestB = null;
            float bestDist = -1f;

            int sampleSize = Mathf.Min(validSegments.Count, 200);

            for (int i = 0; i < sampleSize; i++)
            {
                for (int j = i + 1; j < sampleSize; j++)
                {
                    LaneSegment segA = validSegments[i];
                    LaneSegment segB = validSegments[j];

                    Vector3 posA = mapVisualizer.SegmentDirectory[segA].transform.position;
                    Vector3 posB = mapVisualizer.SegmentDirectory[segB].transform.position;

                    float dist = Vector3.Distance(posA, posB);

                    if (dist > bestDist)
                    {
                        bestDist = dist;
                        bestA = segA;
                        bestB = segB;
                    }
                }
            }

            return (bestA, bestB);
        }*/
        private (LaneSegment, LaneSegment) GetTwoFarStations()
        {
            List<LaneSegment> validSegments = new();

            foreach (Road road in mapData.Roads)
            {
                List<Lane> allLanes = new();

                allLanes.AddRange(road.LanesTowardsA);
                allLanes.AddRange(road.LanesTowardsB);

                foreach (Lane lane in allLanes)
                {
                    for (int i = 0; i < lane.Segments.Count; i++)
                    {
                        LaneSegment segment = lane.Segments[i];

                        if (!mapVisualizer.SegmentDirectory.TryGetValue(segment, out VisualSegment visual))
                            continue;

                        bool isOuter =
                            visual.IsLeftmost ||
                            visual.IsRightmost;

                        if (!isOuter)
                            continue;

                        // Skip first 2 and last 2 segments
                        if (i < 3)
                            continue;

                        if (i >= lane.Segments.Count - 3)
                            continue;

                        validSegments.Add(segment);
                    }
                }
            }

            if (validSegments.Count < 2)
                throw new InvalidOperationException("Not enough outer-lane segments.");

            LaneSegment stationA =
                validSegments[rng.Next(validSegments.Count)];

            Vector3 posA =
                mapVisualizer.SegmentDirectory[stationA].transform.position;

            List<LaneSegment> farEnough = new();

            foreach (LaneSegment seg in validSegments)
            {
                Vector3 pos =
                    mapVisualizer.SegmentDirectory[seg].transform.position;

                float dist = Vector3.Distance(posA, pos);

                if (dist > 40f)
                {
                    farEnough.Add(seg);
                }
            }

            if (farEnough.Count == 0)
            {
                LaneSegment fallback =
                    validSegments[rng.Next(validSegments.Count)];

                return (stationA, fallback);
            }

            LaneSegment stationB =
                farEnough[rng.Next(farEnough.Count)];

            return (stationA, stationB);
        }

        public void ConfigurePlayerSpawn(bool spawnSnowPlow, bool spawnBus)
        {
            if (isInitialized)
            {
                throw new InvalidOperationException("Cannot configure player spawn after VehicleSpawner initialization.");
            }

            spawnPlayerSnowPlowOnStart = spawnSnowPlow;
            spawnPlayerBusOnStart = spawnBus;
        }
        private void SpawnPlayerVehicle(ulong clientId)
        {
            Player player =
                GameManager.Instance.Players.Find(
                    p => p.OwnerClientId == clientId);

            if (player == null)
            {
                Debug.LogError($"No player found for client {clientId}");
                return;
            }

            switch (player.Role)
            {
                case PlayerRole.SnowPlowDriver:
                    SpawnPlayerSnowPlow(clientId);
                    break;

                case PlayerRole.BusDriver:
                    SpawnPlayerBus(clientId);
                    break;

                default:
                    Debug.LogWarning($"Unsupported role: {player.Role}");
                    break;
            }
        }
        public Bus SpawnPlayerBus(ulong clientId)
        {
            EnsureInitialized();

            Player player =
                GameManager.Instance.Players.Find(
                    p => p.OwnerClientId == clientId
                );

            if (player == null)
            {
                Debug.LogError($"No player found for client {clientId}");
                return null;
            }

            Bus playerBus = null;

            foreach (Vehicle vehicle in player.Vehicles)
            {
                if (vehicle is Bus bus)
                {
                    playerBus = bus;
                    break;
                }
            }

            if (playerBus == null)
            {
                Debug.LogError($"Player {player.Name} has no Bus!");
                return null;
            }

            LanePosition startPosition = GetRandomFreePosition();

            playerBus.CurrentPosition = startPosition;
            (LaneSegment stationA, LaneSegment stationB) = GetTwoFarStations();

            playerBus.StationA = stationA;
            playerBus.StationB = stationB;

            MarkStationWithNeighbors(stationA);
            MarkStationWithNeighbors(stationB);
            SpawnStationsClientRpc(
    mapVisualizer.SegmentDirectory[stationA].gameObject.name,
    mapVisualizer.SegmentDirectory[stationB].gameObject.name
);

            GameObject instance = InstantiateVehiclePrefab(
                busPrefab,
                startPosition,
                "PlayerBus",
                clientId
            );

            NPCVehicleBehaviour npcBehaviour =
                instance.GetComponent<NPCVehicleBehaviour>();

            if (npcBehaviour != null)
            {
                npcBehaviour.enabled = false;
            }

            NPCVehicleMover npcMover =
                instance.GetComponent<NPCVehicleMover>();

            if (npcMover != null)
            {
                npcMover.enabled = false;
            }

            VehicleSegmentSensor sensor =
                instance.GetComponent<VehicleSegmentSensor>();

            if (sensor == null)
            {
                sensor = instance.AddComponent<VehicleSegmentSensor>();
            }

            sensor.Initialize(playerBus);

            occupancyManager.RegisterVehicle(
                playerBus,
                startPosition
            );

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                CameraFollow cameraFollow =
                    Camera.main.GetComponent<CameraFollow>();

                if (cameraFollow != null)
                {
                    cameraFollow.SetTarget(instance.transform);
                }
            }

            BusMovement busMovement = instance.GetComponent<BusMovement>();

            if (busMovement != null)
            {
                

                busMovement.SetStations(
                    mapVisualizer.SegmentDirectory[stationA],
                    mapVisualizer.SegmentDirectory[stationB]
                );

                busMovement.SetBusModel(playerBus);
            }
            StationArrowIndicator arrowIndicator =
    instance.GetComponent<StationArrowIndicator>();

            if (arrowIndicator != null)
            {
                arrowIndicator.SetStations(
                    mapVisualizer.SegmentDirectory[stationA],
                    mapVisualizer.SegmentDirectory[stationB]
                );
            }

            return playerBus;
        }

        [ClientRpc]
        private void SpawnStationsClientRpc(
    string stationAName,
    string stationBName
        )
        {
            if (IsServer)
                return;

            StartCoroutine(
                SpawnStationsWhenReady(
                    stationAName,
                    stationBName
                )
            );
        }

        private IEnumerator SpawnStationsWhenReady(
    string stationAName,
    string stationBName
)
        {
            Debug.Log("CLIENT waiting for station visuals...");
            VisualSegment[] allSegments = null;

            while (true)
            {
                allSegments = FindObjectsOfType<VisualSegment>();

                bool ready =
                    allSegments.Length > 0 &&
                    allSegments.All(v =>
                        v != null &&
                        v.LanePosition != null);

                if (ready)
                    break;

                yield return null;
            }

            foreach (VisualSegment vs in allSegments)
            {
                if (vs.gameObject.name == stationAName ||
                    vs.gameObject.name == stationBName)
                {
                    LaneSegment seg =
                        vs.LanePosition.Lane[
                            vs.LanePosition.SegmentIndex];

                    MarkStationWithNeighbors(seg);
                }
            }
            Debug.Log("CLIENT station visuals spawned");

            // várjuk meg amíg a networkelt bus prefab ténylegesen megjelenik
            while (FindObjectsOfType<BusMovement>().Length == 0)
            {
                yield return null;
            }

            BusMovement[] buses = FindObjectsOfType<BusMovement>();

            foreach (BusMovement bus in buses)
            {
                StationArrowIndicator arrows =
                    bus.GetComponent<StationArrowIndicator>();

                if (arrows != null)
                {
                    arrows.SetStations(
                        mapVisualizer.SegmentDirectory[
                            GetSegmentByName(stationAName)],
                        mapVisualizer.SegmentDirectory[
                            GetSegmentByName(stationBName)]
                    );
                }
            }
        }
        private LaneSegment GetSegmentByName(string objName)
        {
            foreach (var kvp in mapVisualizer.SegmentDirectory)
            {
                if (kvp.Value.gameObject.name == objName)
                    return kvp.Key;
            }

            return null;
        }
    }
}