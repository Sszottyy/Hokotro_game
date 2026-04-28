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
            SyncOccupancyWithCurrentPosition();

            // ha a sensor szerint mar elertunk egy path elemet,
            // akkor a mover ne akarjon visszamenni oda
            mover.SyncWithCurrentPosition(vehicle.CurrentPosition);

            // ha celba ert, akkor uj celt keres
            if (HasReachedDestination())
            {
                TryUpdateDestination();
                RecalculatePath();
                return;
            }

            // ha nincs utvonala, akkor keres egyet
            if (currentPath == null || currentPath.Count == 0)
            {
                RecalculatePath();
                return;
            }

            // ha a kovetkezo szegmens foglalt, akkor a mover megall
            // ilyenkor nem toroljuk az utvonalat, csak varunk
            LanePosition nextPosition = mover.GetCurrentTargetPosition();

            if (nextPosition != null && !occupancyManager.CanEnter(vehicle, nextPosition))
            {
                mover.PauseMovement();
                return;
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

        private void SyncOccupancyWithCurrentPosition()
        {
            if (vehicle.CurrentPosition == null) return;

            if (lastKnownPosition == null || !lastKnownPosition.Equals(vehicle.CurrentPosition))
            {
                occupancyManager.UpdateVehiclePosition(vehicle, vehicle.CurrentPosition);
                lastKnownPosition = vehicle.CurrentPosition;
            }
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
                return;
            }

            currentPath = Pathfinder.FindPath(
                vehicle.CurrentPosition,
                destination,
                traversalPolicy
            );

            if (currentPath.Count == 0)
            {
                mover.ClearPath();
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
    }
}