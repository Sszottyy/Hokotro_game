using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Vehicles;
using SnowPlow.Model.Tools;
using SnowPlowVehicle = SnowPlow.Model.Vehicles.SnowPlow;

namespace SnowPlow.Model.Players
{
    public class Player
    {
        public string Name { get; set; }

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
            Vehicles.Add(vehicle);
            _team?.AddVehicle(vehicle);
            if (vehicle is SnowPlowVehicle snowPlow)
            {
                snowPlow.SnowCleared += HandleClearedSnow;
            }
        }
        

        public void RemoveVehicle(Vehicle vehicle)
        {
            Vehicles.Remove(vehicle);
            _team?.RemoveVehicle(vehicle);
            if (vehicle is SnowPlowVehicle snowPlow)
            {
                snowPlow.SnowCleared -= HandleClearedSnow;
            }
        }
        public Player() { }

        public Player(string name, Team team)
        {
            Name = name;
            Team = team;
        }
        private void HandleClearedSnow()
        {
            Team.Money += 1; // Example reward for clearing snow
        }
        private void HandleBusEvent()
        {
            Team.Score += 1;
        }
    }
}
