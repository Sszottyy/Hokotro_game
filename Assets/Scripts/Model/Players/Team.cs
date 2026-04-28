using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Vehicles;

namespace SnowPlow.Model.Players
{
    public class Team
    {
        public string Name { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public List<Vehicle> Vehicles { get; set; } = new List<Vehicle>();


        public int Score { get; set; }

        public int Money { get; set; }

        public void AddPlayer(Player player)
        {
            Players.Add(player);
        }

        public void RemovePlayer(Player player)
        {
            Players.Remove(player);
        }
        
        public void AddVehicle(Vehicle vehicle)
        {
            Vehicles.Add(vehicle);
        }

        public void RemoveVehicle(Vehicle vehicle)
        {
            Vehicles.Remove(vehicle);
        }
    }
}
