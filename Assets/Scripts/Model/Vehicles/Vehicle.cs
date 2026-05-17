using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Players;

namespace SnowPlow.Model.Vehicles
{
    public abstract class Vehicle
    {
        static int idCounter = 0;

        public LanePosition CurrentPosition {  get; set; }
        public Player Owner { get; set; }
        public bool isBlocked { get; set; }
        private string id { get;}
        public Vehicle()
        {
            
            id = $"V-{idCounter++}";
            isBlocked = false;
        }
    }
}
