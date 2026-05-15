using SnowPlow.Model.Map;
using SnowPlow.Model.Vehicles;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnowPlow.Controller.Traffic
{
    public class VehicleOccupancyManager : MonoBehaviour
    {
        private readonly Dictionary<Vehicle, LanePosition> vehiclePositions = new();
        private readonly Dictionary<LanePosition, Vehicle> occupiedPositions = new();
        private readonly Dictionary<Vehicle, LanePosition> vehicleReservations = new();
        private readonly Dictionary<LanePosition, Vehicle> reservedPositions = new();

        public bool TryRegisterVehicle(Vehicle vehicle, LanePosition startPosition)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));
            if (startPosition == null) throw new ArgumentNullException(nameof(startPosition));

            if (vehiclePositions.ContainsKey(vehicle))
            {
                return TryUpdateVehiclePosition(vehicle, startPosition);
            }

            if (occupiedPositions.TryGetValue(startPosition, out Vehicle owner) && owner != vehicle)
            {
                return false;
            }

            vehiclePositions[vehicle] = startPosition;
            occupiedPositions[startPosition] = vehicle;

            return true;
        }

        public void RegisterVehicle(Vehicle vehicle, LanePosition startPosition)
        {
            bool success = TryRegisterVehicle(vehicle, startPosition);

            if (!success)
            {
                throw new InvalidOperationException(
                    $"Cannot register vehicle at occupied position: {startPosition}"
                );
            }
        }

        public bool TryUpdateVehiclePosition(Vehicle vehicle, LanePosition newPosition)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));
            if (newPosition == null) throw new ArgumentNullException(nameof(newPosition));

            if (occupiedPositions.TryGetValue(newPosition, out Vehicle newOwner) && newOwner != vehicle)
            {
                return false;
            }

            if (reservedPositions.TryGetValue(newPosition, out Vehicle reservedBy) && reservedBy != vehicle)
            {
                return false;
            }

            if (vehiclePositions.TryGetValue(vehicle, out LanePosition oldPosition))
            {
                if (occupiedPositions.TryGetValue(oldPosition, out Vehicle oldOwner) && oldOwner == vehicle)
                {
                    occupiedPositions.Remove(oldPosition);
                }
            }

            vehiclePositions[vehicle] = newPosition;
            occupiedPositions[newPosition] = vehicle;

            ClearReservation(vehicle);

            return true;
        }

        public void UpdateVehiclePosition(Vehicle vehicle, LanePosition newPosition)
        {
            bool success = TryUpdateVehiclePosition(vehicle, newPosition);

            if (!success)
            {
                throw new InvalidOperationException(
                    $"Cannot move vehicle to occupied position: {newPosition}"
                );
            }
        }

        public void UnregisterVehicle(Vehicle vehicle)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));
            ClearReservation(vehicle);

            if (!vehiclePositions.TryGetValue(vehicle, out LanePosition position))
            {
                return;
            }

            if (occupiedPositions.TryGetValue(position, out Vehicle owner) && owner == vehicle)
            {
                occupiedPositions.Remove(position);
            }

            vehiclePositions.Remove(vehicle);
        }

        public bool CanEnter(Vehicle vehicle, LanePosition position)
        {
            if (vehicle == null) return false;
            if (position == null) return false;

            if (occupiedPositions.TryGetValue(position, out Vehicle owner) && owner != vehicle)
            {
                return false;
            }

            if (reservedPositions.TryGetValue(position, out Vehicle reserver) && reserver != vehicle)
            {
                return false;
            }

            return true;
        }

        public bool IsOccupied(LanePosition position)
        {
            if (position == null) return false;

            return occupiedPositions.ContainsKey(position) || reservedPositions.ContainsKey(position);
        }

        public bool TryGetVehicleAt(LanePosition position, out Vehicle vehicle)
        {
            vehicle = null;

            if (position == null)
            {
                return false;
            }

            return occupiedPositions.TryGetValue(position, out vehicle);
        }

        public bool TryGetPositionOf(Vehicle vehicle, out LanePosition position)
        {
            position = null;

            if (vehicle == null)
            {
                return false;
            }

            return vehiclePositions.TryGetValue(vehicle, out position);
        }

        public bool TryReservePosition(Vehicle vehicle, LanePosition position)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));
            if (position == null) throw new ArgumentNullException(nameof(position));

            if (vehiclePositions.TryGetValue(vehicle, out LanePosition currentPosition) &&
                currentPosition.Equals(position))
            {
                return true;
            }

            if (occupiedPositions.TryGetValue(position, out Vehicle occupiedBy) && occupiedBy != vehicle)
            {
                return false;
            }

            if (reservedPositions.TryGetValue(position, out Vehicle reservedBy) && reservedBy != vehicle)
            {
                return false;
            }

            ClearReservation(vehicle);

            vehicleReservations[vehicle] = position;
            reservedPositions[position] = vehicle;

            return true;
        }

        public void ClearReservation(Vehicle vehicle)
        {
            if (vehicle == null) return;

            if (!vehicleReservations.TryGetValue(vehicle, out LanePosition reservedPosition))
            {
                return;
            }

            if (reservedPositions.TryGetValue(reservedPosition, out Vehicle owner) && owner == vehicle)
            {
                reservedPositions.Remove(reservedPosition);
            }

            vehicleReservations.Remove(vehicle);
        }
    }
}