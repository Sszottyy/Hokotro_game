using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Model.Vehicles
{
    public class Bus : Vehicle
    {
        public List<MapNode> stations { get; set; }
        public int currentTargetIndex { get; set; }
        public int completedTrips { get; set; }

        public Bus(List<MapNode> stations)
        {
            if (stations == null) throw new ArgumentNullException(nameof(stations));
            if (stations.Count < 2) throw new ArgumentException("A bus must have at least two stations.", nameof(stations));
            this.stations = stations;
            currentTargetIndex = 0;
            completedTrips = 0;

        }
    }
}
