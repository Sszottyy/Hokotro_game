using SnowPlow.Model.Tools;
using SnowPlow.Model.Vehicles;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine; //alias, mivel a namespace es a jarmu is SnowPlow
using SnowPlowVehicle = SnowPlow.Model.Vehicles.SnowPlow;

namespace SnowPlow.Model.Players
{

    public enum PlayerRole
    {
        None,
        SnowPlowDriver,
        BusDriver
    }
    public class Player
    {
        public string Name { get; set; }
        public PlayerRole Role { get; set; } = PlayerRole.None;

        private Team _team;
        public Team Team
        {
            get
            {
                return _team;
            }

            set
            {
                _team?.RemovePlayer(this);
                _team = value;
                _team?.AddPlayer(this);
            }
        }

        public List<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public List<IPlowTool> PlowTools { get; set; } = new List<IPlowTool>();

        public void AddVehicle(Vehicle vehicle)
        {

            Debug.Log("AddVehicle called");
            if (vehicle == null) return;
            Vehicles.Add(vehicle);
            _team?.AddVehicle(vehicle);
            if (vehicle is SnowPlowVehicle snowPlow)
            {
                snowPlow.SnowCleared += HandleClearedSnow;
            }
            if (vehicle is Bus bus)
            {
                bus.TripCompleted += HandleBusTripCompleted;
                bus.PassengersDroppedOff += HandlePassengersDroppedOff;
            }
        }

        public void RemoveVehicle(Vehicle vehicle)
        {
            if (vehicle == null) return;
            Vehicles.Remove(vehicle);
            _team?.RemoveVehicle(vehicle);
            if (vehicle is SnowPlowVehicle snowPlow)
            {
                snowPlow.SnowCleared -= HandleClearedSnow;
            }
            if (vehicle is Bus bus)
            {
                bus.TripCompleted -= HandleBusTripCompleted;
                bus.PassengersDroppedOff -= HandlePassengersDroppedOff;
            }
        }
        private void HandleClearedSnow()
        {
            Team?.AddMoney(1); // Example reward for clearing snow
        }
        private void HandleBusTripCompleted(int score)
        {
            Debug.Log($"Trip completed in time → +{score} score");
            Team.Score += score;
        }

        private void HandlePassengersDroppedOff(int amount)
        {
            Debug.Log($"Passengers dropped off: +{amount} score");
            Team.Score += amount;
        }

        public SnowPlowVehicle GetOwnedSnowPlow()
        {
            foreach (Vehicle vehicle in Vehicles)
            {
                if (vehicle is SnowPlowVehicle snowPlow)
                {
                    return snowPlow;
                }
            }

            return null;
        }

        public void AddPlowTool(IPlowTool tool)
        {
            if (tool == null) return;

            if (HasTool(tool.Type())) return;

            PlowTools.Add(tool);
        }

        public bool HasTool(PlowToolType type)
        {
            return FindOwnedTool(type) != null;
        }

        public IPlowTool FindOwnedTool(PlowToolType type)
        {
            foreach (IPlowTool tool in PlowTools)
            {
                if (tool != null && tool.Type() == type)
                {
                    return tool;
                }
            }

            return null;
        }

        public Player() { }

        public Player(string name, Team team)
        {
            Name = name;
            Team = team;
        }

    }
}
