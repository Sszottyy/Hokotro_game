using SnowPlow.Controller.NPCTargetSelection;
using SnowPlow.Controller.Pathfinding;
using SnowPlow.Controller.Traffic;
using SnowPlow.Model.Map;
using SnowPlow.Model.Map.Generator;
using SnowPlow.Model.Vehicles;
using System;
using System.Collections.Generic;
using UnityEngine;
using SnowPlowVehicle = SnowPlow.Model.Vehicles.SnowPlow;

namespace SnowPlow.Controller.NPCMovement
{
    // ez a class a jarmu agya
    // - hova menjen a jarmu
    // - mikor kell utvonalat keresni
    // - mikor kell ujratervezni
    // - megalljon-e, ha a kovetkezo szegmensen mar van masik jarmu
    public class NPCVehicleBehaviour : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private NPCVehicleMover mover;

        [Header("Traffic")]
        [SerializeField] private VehicleOccupancyManager occupancyManager;

        [Header("Repath")]
        [SerializeField] private float repathInterval = 0.5f;
        [SerializeField] private bool repathIfNextSegmentBlocked = true;

        [SerializeField] private MapVisualizer mapVisualizer;

        public void SetMapVisualizer(MapVisualizer visualizer)
        {
            mapVisualizer = visualizer;
        }

        private MapData mapData;

        private Vehicle vehicle;
        private Car car;
        private SnowPlowVehicle snowPlow;

        private ITraversalPolicy traversalPolicy;

        private LanePosition destination;
        private LanePosition lastKnownPosition;
        private List<LanePosition> currentPath = new();

        private float repathTimer;
        private bool isInitialized;

        private void Awake()
        {
            if (mover == null)
            {
                mover = GetComponent<NPCVehicleMover>();
            }

            if (occupancyManager == null)
            {
                occupancyManager = FindObjectOfType<VehicleOccupancyManager>();
            }
        }

        // ezt hivja meg az a kod, ami letrehozza az NPC-t
        // azert kell, mert a Car es SnowPlow nalatok nem MonoBehaviour, hanem sima modell class
        public void Initialize(Vehicle vehicleModel, MapData map)
        {
            if (vehicleModel == null) throw new ArgumentNullException(nameof(vehicleModel));
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (mover == null) throw new InvalidOperationException("NPCVehicleMover is missing.");
            if (occupancyManager == null) throw new InvalidOperationException("VehicleOccupancyManager is missing from the scene.");

            vehicle = vehicleModel;
            mapData = map;

            car = vehicle as Car;
            snowPlow = vehicle as SnowPlowVehicle;

            if (car != null)
            {
                traversalPolicy = new CarTraversalPolicy();
            }
            else if (snowPlow != null)
            {
                traversalPolicy = new SnowPlowTraversalPolicy();
            }
            else
            {
                throw new InvalidOperationException("This vehicle type is not supported as NPC.");
            }

            if (vehicle.CurrentPosition != null)
            {
                lastKnownPosition = vehicle.CurrentPosition;
                occupancyManager.RegisterVehicle(vehicle, vehicle.CurrentPosition);
            }

            isInitialized = true;

            TryUpdateDestination();
            RecalculatePath();
        }

        private void Update()
        {
            if (!isInitialized) return;

            repathTimer += Time.deltaTime;

            // ha a sensor meg nem allitotta be a logikai poziciot, akkor nem tudunk honnan utat tervezni
            if (vehicle.CurrentPosition == null) return;

            // ha a sensor miatt megvaltozott a CurrentPosition,
            // akkor frissitjuk a foglaltsagi rendszert is
            if (!SyncOccupancyWithCurrentPosition())
            {
                return;
            }

            // ha a sensor szerint mar elertunk egy path elemet,
            // akkor a mover ne akarjon visszamenni oda

            mover.SyncWithCurrentPosition(vehicle.CurrentPosition);

            if (mover.IsStunned)
            {
                occupancyManager.ClearReservation(vehicle);
                return;
            }

            // ha celba ert, akkor uj celt keres
            if (HasReachedDestination())
            {
                mover.ClearPath();
                occupancyManager.ClearReservation(vehicle);

                if (repathTimer >= repathInterval)
                {
                    TryUpdateDestination();
                    RecalculatePath();
                }

                return;
            }

            // ha nincs utvonala, akkor keres egyet
            if (currentPath == null || currentPath.Count == 0)
            {
                mover.ClearPath();
                occupancyManager.ClearReservation(vehicle);

                if (repathTimer >= repathInterval)
                {
                    TryUpdateDestination();
                    RecalculatePath();
                }

                return;
            }

            // ha a kovetkezo szegmens foglalt, akkor a mover megall
            // ilyenkor nem toroljuk az utvonalat, csak varunk
            LanePosition nextPosition = mover.GetCurrentTargetPosition();

            if (nextPosition != null && !nextPosition.Equals(vehicle.CurrentPosition))
            {
                if (!occupancyManager.TryReservePosition(vehicle, nextPosition))
                {
                    mover.PauseMovement();

                    if (repathTimer >= repathInterval)
                    {
                        if (snowPlow != null)
                        {
                            TryRecalculateSnowPlowPathAvoidingOccupied();
                        }
                        else
                        {
                            RecalculatePath();
                        }
                    }

                    return;
                }
            }

            mover.ResumeMovement();

            // ha a kovetkezo szakasz kozben jarhatatlanna valt, akkor ujratervez
            if (repathIfNextSegmentBlocked && repathTimer >= repathInterval && IsNextPathStepBlocked())
            {
                RecalculatePath();
                return;
            }

            // ha a mover mar vegzett az uttal, de a modell szerint meg nincs celban,
            // akkor valami miatt uj ut kell
            if (!mover.HasPath && !HasReachedDestination())
            {
                RecalculatePath();
            }
        }

        private void OnDestroy()
        {
            if (occupancyManager != null && vehicle != null)
            {
                occupancyManager.UnregisterVehicle(vehicle);
            }
        }

        private bool SyncOccupancyWithCurrentPosition()
        {
            if (vehicle.CurrentPosition == null) return false;

            if (lastKnownPosition == null || !lastKnownPosition.Equals(vehicle.CurrentPosition))
            {
                bool success = occupancyManager.TryUpdateVehiclePosition(vehicle, vehicle.CurrentPosition);

                if (!success)
                {
                    vehicle.CurrentPosition = lastKnownPosition;
                    currentPath.Clear();
                    mover.ClearPath();
                    mover.PauseMovement();
                    occupancyManager.ClearReservation(vehicle);
                    return false;
                }

                lastKnownPosition = vehicle.CurrentPosition;
            }

            return true;
        }

        private void TryUpdateDestination()
        {
            if (car != null)
            {
                CarTargetSelector.UpdateTargets(car, mapData);
                destination = car.Destination;
                return;
            }

            if (snowPlow != null)
            {
                if (snowPlow.CurrentPosition == null) return;

                destination = SnowPlowTargetSelector.SelectClosestTarget(
                    snowPlow.CurrentPosition,
                    snowPlow.EquippedTool,
                    traversalPolicy
                );
            }
        }

        private void RecalculatePath()
        {
            repathTimer = 0f;

            if (vehicle.CurrentPosition == null || destination == null)
            {
                currentPath.Clear();
                mover.ClearPath();
                occupancyManager.ClearReservation(vehicle);
                return;
            }

            currentPath = Pathfinder.FindPath(
                vehicle.CurrentPosition,
                destination,
                traversalPolicy
            );

            if (currentPath.Count == 0 && car != null)
            {
                currentPath = FindBestReachablePathTowardDestination(
                    vehicle.CurrentPosition,
                    destination
                );
            }

            if (currentPath.Count == 0)
            {
                mover.ClearPath();
                occupancyManager.ClearReservation(vehicle);
                return;
            }

            mover.SetPath(currentPath);
        }

        private bool IsNextPathStepBlocked()
        {
            LanePosition nextPosition = mover.GetCurrentTargetPosition();

            if (vehicle.CurrentPosition == null) return false;
            if (nextPosition == null) return false;

            return !traversalPolicy.CanTransition(vehicle.CurrentPosition, nextPosition);
        }

        private bool HasReachedDestination()
        {
            if (vehicle.CurrentPosition == null) return false;
            if (destination == null) return false;

            return vehicle.CurrentPosition.Equals(destination);
        }

        private List<LanePosition> FindBestReachablePathTowardDestination(
    LanePosition start,
    LanePosition target)
        {
            List<LanePosition> empty = new();

            if (mapVisualizer == null) return empty;
            if (!TryGetWorldPosition(target, out Vector3 targetWorld)) return empty;

            Queue<LanePosition> queue = new();
            HashSet<LanePosition> visited = new();
            Dictionary<LanePosition, LanePosition> cameFrom = new();

            queue.Enqueue(start);
            visited.Add(start);

            LanePosition best = start;
            float bestDistance = float.PositiveInfinity;

            while (queue.Count > 0)
            {
                LanePosition current = queue.Dequeue();

                if (!current.Equals(start) && TryGetWorldPosition(current, out Vector3 currentWorld))
                {
                    float distance = (currentWorld - targetWorld).sqrMagnitude;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = current;
                    }
                }

                foreach (LanePosition neighbor in Pathfinder.GetNeighbors(current, traversalPolicy))
                {
                    if (visited.Contains(neighbor)) continue;

                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            if (best.Equals(start))
            {
                return empty;
            }

            return ReconstructPath(cameFrom, best);
        }

        private List<LanePosition> ReconstructPath(
            Dictionary<LanePosition, LanePosition> cameFrom,
            LanePosition current)
        {
            List<LanePosition> path = new() { current };

            while (cameFrom.TryGetValue(current, out LanePosition previous))
            {
                current = previous;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private bool TryGetWorldPosition(LanePosition position, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (position == null) return false;
            if (position.Lane == null) return false;
            if (position.SegmentIndex < 0) return false;
            if (position.SegmentIndex >= position.Lane.Segments.Count) return false;
            if (mapVisualizer == null) return false;

            LaneSegment segment = position.Lane[position.SegmentIndex];

            if (!mapVisualizer.SegmentDirectory.TryGetValue(segment, out VisualSegment visualSegment))
            {
                return false;
            }

            if (visualSegment == null) return false;

            worldPosition = visualSegment.transform.position;
            return true;
        }

        private bool TryRecalculateSnowPlowPathAvoidingOccupied()
        {
            repathTimer = 0f;

            if (snowPlow == null) return false;
            if (vehicle.CurrentPosition == null) return false;

            occupancyManager.ClearReservation(vehicle);

            ITraversalPolicy occupancyAwarePolicy =
                new OccupancyAwareTraversalPolicy(
                    traversalPolicy,
                    occupancyManager,
                    vehicle
                );

            LanePosition newDestination = SnowPlowTargetSelector.SelectClosestTarget(
                snowPlow.CurrentPosition,
                snowPlow.EquippedTool,
                occupancyAwarePolicy
            );

            if (newDestination == null)
            {
                currentPath.Clear();
                mover.ClearPath();
                return false;
            }

            List<LanePosition> newPath = Pathfinder.FindPath(
                vehicle.CurrentPosition,
                newDestination,
                occupancyAwarePolicy
            );

            if (newPath.Count == 0)
            {
                currentPath.Clear();
                mover.ClearPath();
                return false;
            }

            destination = newDestination;
            currentPath = newPath;
            mover.SetPath(currentPath);
            mover.PauseMovement();

            return true;
        }

        private sealed class OccupancyAwareTraversalPolicy : ITraversalPolicy
        {
            private readonly ITraversalPolicy basePolicy;
            private readonly VehicleOccupancyManager occupancyManager;
            private readonly Vehicle vehicle;

            public OccupancyAwareTraversalPolicy(
                ITraversalPolicy basePolicy,
                VehicleOccupancyManager occupancyManager,
                Vehicle vehicle)
            {
                this.basePolicy = basePolicy ?? throw new ArgumentNullException(nameof(basePolicy));
                this.occupancyManager = occupancyManager ?? throw new ArgumentNullException(nameof(occupancyManager));
                this.vehicle = vehicle ?? throw new ArgumentNullException(nameof(vehicle));
            }

            public bool CanEnterSegment(LanePosition position)
            {
                return basePolicy.CanEnterSegment(position)
                    && occupancyManager.CanEnter(vehicle, position);
            }

            public bool CanTransition(LanePosition from, LanePosition to)
            {
                return basePolicy.CanTransition(from, to)
                    && occupancyManager.CanEnter(vehicle, to);
            }

            public float GetTraversalCost(LanePosition from, LanePosition to)
            {
                if (!CanTransition(from, to))
                {
                    return float.PositiveInfinity;
                }

                return basePolicy.GetTraversalCost(from, to);
            }
        }
    }
}