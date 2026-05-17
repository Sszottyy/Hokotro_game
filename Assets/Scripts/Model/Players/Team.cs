using SnowPlow.Model.Vehicles;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlow.Model.Players
{
    public class Team
    {
        public string Name { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public List<Vehicle> Vehicles { get; set; } = new List<Vehicle>();


        public int Score { get; set; }

        public int Money { get; private set; }

        public void AddPlayer(Player player)
        {
            if (player == null) return;
            if (Players.Contains(player)) return;
            Players.Add(player);
        }

        public void RemovePlayer(Player player)
        {
            if (player == null) return;
            Players.Remove(player);
        }
        
        public void AddVehicle(Vehicle vehicle)
        {
            if (vehicle == null) return;
            if (Vehicles.Contains(vehicle)) return;
            Vehicles.Add(vehicle);
        }

        public void RemoveVehicle(Vehicle vehicle)
        {
            if (vehicle == null) return;
            Vehicles.Remove(vehicle);
        }

        public bool CanAfford(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            return Money >= amount;
        }

        public bool TrySpendMoney(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            if (!CanAfford(amount)) return false;

            Money -= amount;
            return true;
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0) return;

            Money += amount;
            Debug.Log($"[TEAM MONEY] {Name} now has {Money}");
            if (NetworkManager.Singleton != null &&
    NetworkManager.Singleton.IsServer &&
    TeamMoneySync.Instance != null)
            {
                TeamMoneySync.Instance.SyncMoney();
            }
        }
        public void SetMoney(int amount)
        {
            Money = amount;
        }
    }
}
