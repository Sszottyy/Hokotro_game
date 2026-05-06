using SnowPlow.Controller.NPCMovement;
using SnowPlow.Controller.Traffic;
using SnowPlow.Model.Map;
using SnowPlow.Model.Map.Generator;
using SnowPlow.Model.Vehicles;
using System;
using System.Collections.Generic;
using UnityEngine;
using SnowPlow.Model.Tools;
using SnowPlowVehicle = SnowPlow.Model.Vehicles.SnowPlow;

namespace SnowPlow.Controller.Spawning
{
    // ez a class felel a jarmuvek spawnolasert
    // nem pathfindingol, nem mozgat, nem valaszt celt
    // csak letrehozza a modelt + a prefab GameObjectet, es osszekoti oket
    public class VehicleSpawner : MonoBehaviour
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

            mapData = data;
            mapVisualizer = visualizer;
            isInitialized = true;

            SpawnInitialVehicles();
        }

        // jatek eleji spawn
        // most: 6 auto + 1 jatekos hokotro
        // NPC hokotro alapbol NEM spawnol, azt majd shop hivja
        private void SpawnInitialVehicles()
        {
            for (int i = 0; i < initialCarCount; i++)
            {
                SpawnCarNPC();
            }

            if (spawnPlayerSnowPlowOnStart)
            {
                SpawnPlayerSnowPlow();
            }
            if (spawnPlayerBusOnStart)
            {
                SpawnBus();
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

            behaviour.Initialize(car, mapData);
            return car;
        }

        // ezt majd a shop hivja, amikor veszunk egy NPC hokotrot
        public SnowPlowVehicle SpawnSnowPlowNPC()
        {
            return SpawnSnowPlowNPC(new SweaperTool());
        }

        public SnowPlowVehicle SpawnSnowPlowNPC(IPlowTool tool)
        {
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

            behaviour.Initialize(snowPlow, mapData);

            return snowPlow;
        }

        public SnowPlowVehicle SpawnPlayerSnowPlow()
        {
            EnsureInitialized();

            SnowPlowVehicle playerSnowPlow = new();
            LanePosition startPosition = GetRandomFreePosition();

            playerSnowPlow.CurrentPosition = startPosition;

            GameObject instance = InstantiateVehiclePrefab(
                playerSnowPlowPrefab,
                startPosition,
                "PlayerCar"
            );

            NPCVehicleBehaviour npcBehaviour = instance.GetComponent<NPCVehicleBehaviour>();
            if (npcBehaviour != null)
            {
                npcBehaviour.enabled = false;
            }

            NPCVehicleMover npcMover = instance.GetComponent<NPCVehicleMover>();
            if (npcMover != null)
            {
                npcMover.enabled = false;
            }

            VehicleSegmentSensor sensor = instance.GetComponent<VehicleSegmentSensor>();
            if (sensor == null)
            {
                sensor = instance.AddComponent<VehicleSegmentSensor>();
            }

            sensor.Initialize(playerSnowPlow);

            if (global::GameManager.Instance != null && global::GameManager.Instance.CurrentPlayer != null)
            {
                var player = global::GameManager.Instance.CurrentPlayer;

                IPlowTool sweaperTool = playerSnowPlow.EquippedTool;

                if (sweaperTool == null || sweaperTool.Type() != PlowToolType.Sweaper)
                {
                    sweaperTool = new SweaperTool();
                }

                if (!player.HasTool(PlowToolType.Sweaper))
                {
                    player.AddPlowTool(sweaperTool);
                }
                else
                {
                    sweaperTool = player.FindOwnedTool(PlowToolType.Sweaper);
                }

                playerSnowPlow.EquippedTool = sweaperTool;
                player.AddVehicle(playerSnowPlow);
            }

            occupancyManager.RegisterVehicle(playerSnowPlow, startPosition);

            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(instance.transform);
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

            mapVisualizer.SegmentDirectory[stationA].MarkAsStation();
            mapVisualizer.SegmentDirectory[stationB].MarkAsStation();

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

            return bus;
        }

        private GameObject InstantiateVehiclePrefab(GameObject prefab, LanePosition startPosition, string objectName)
        {
            Vector3 worldPosition = GetWorldPosition(startPosition);

            GameObject instance = Instantiate(
                prefab,
                worldPosition,
                Quaternion.identity,
                transform
            );

            instance.name = objectName;
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
        private (LaneSegment, LaneSegment) GetTwoFarStations()
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
                    if (index >= 2 && index <= maxIndex - 2)
                        validSegments.Add(seg);
                }
            }

            if (validSegments.Count < 2)
                throw new InvalidOperationException("Not enough valid segments to place two stations.");

            LaneSegment bestA = null;
            LaneSegment bestB = null;
            float bestDist = -1f;

            int sampleSize = Mathf.Min(validSegments.Count, 150);

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
    }
}