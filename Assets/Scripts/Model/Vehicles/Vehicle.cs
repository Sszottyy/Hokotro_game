using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Model.Vehicles
{
    public abstract class Vehicle
    {
        static int idCounter = 0;

        public Lane CurrentLane {  get; set; }

        public bool isBlocked { get; set; }
        private string id { get;}
        public Vehicle()
        {
            
            id = $"V-{idCounter++}";
            isBlocked = false;
        }
    }
}
