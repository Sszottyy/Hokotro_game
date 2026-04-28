using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Model.Vehicles
{
    public class Bus : Vehicle
    {
        //stations and target moved to controller
        public int CompletedTrips { get; set; }

        public Bus()
        {
            CompletedTrips = 0;

        }
        public void BusOnSegment(LaneSegment segment)
        {

            if (segment.HasIce)
            { }
            if (segment.SnowLevel >= 3)
            {
                this.isBlocked = true;
            }
        }
    }
}
