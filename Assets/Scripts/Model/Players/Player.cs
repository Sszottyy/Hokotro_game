using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Vehicles;

namespace SnowPlow.Model.Players
{
    public class Player
    {
        public string Name { get; set; }

        public Team Team
        {
            get
            {
                return Team;
            }

            set
            {
                Team?.RemovePlayer(this);
                Team = value;
                Team.AddPlayer(this);
            }
        }

        public List<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public void AddVehicle(Vehicle vehicle)
        {
            Vehicles.Add(vehicle);
            Team?.AddVehicle(vehicle);
        }

        public void RemoveVehicle(Vehicle vehicle)
        {
            Vehicles.Remove(vehicle);
            Team?.RemoveVehicle(vehicle);
        }
        public Player() { }

        public Player(string name, Team team)
        {
            Name = name;
            Team = team;
        }

    }
}
