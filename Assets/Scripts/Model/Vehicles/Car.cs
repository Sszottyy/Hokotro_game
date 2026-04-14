using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Vehicles;

namespace SnowPlow.Model.Vehicles
{
    public class Car : Vehicle
    {
        public MapNode start { get; set; }
        public MapNode destination { get; set; }


        public Car(MapNode start, MapNode destination)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (start == destination) throw new ArgumentException("Start and destination cannot be the same.", nameof(destination));
            this.start = start;
            this.destination = destination;
        }
    }
}
