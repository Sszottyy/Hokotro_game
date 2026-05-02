using SnowPlow.Controller.NPCMovement;
using SnowPlow.Controller.Traffic;
using SnowPlow.Model.Map;
using SnowPlow.Model.Map.Generator;
using SnowPlow.Model.Vehicles;
using System;
using System.Collections.Generic;
using UnityEngine;
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

        [Header("Traffic")]
        [SerializeField] private VehicleOccupancyManager occupancyManager;

        [Header("Initial Spawn")]
        [SerializeField] private int initialCarCount = 6;
        [SerializeField] private bool spawnPlayerSnowPlowOnStart = true;

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

            SpawnSnowPlowNPC();

            if (spawnPlayerSnowPlowOnStart)
            {
                SpawnPlayerSnowPlow();
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
            EnsureInitialized();

            SnowPlowVehicle snowPlow = new();
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
            throw new NotImplementedException("TODO: Bus spawn will be implemented later.");
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
    }
}