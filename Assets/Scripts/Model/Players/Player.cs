using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Vehicles;

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

        public void AddVehicle(Vehicle vehicle)
        {
            Vehicles.Add(vehicle);
            _team?.AddVehicle(vehicle);
        }

        public void RemoveVehicle(Vehicle vehicle)
        {
            Vehicles.Remove(vehicle);
            _team?.RemoveVehicle(vehicle);
        }
        public Player() { }

        public Player(string name, Team team)
        {
            Name = name;
            Team = team;
        }

    }
}
