using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Vehicles;

namespace SnowPlow.Model.Vehicles
{
    public class Car : Vehicle
    {
        public LanePosition Home { get; set; } = null;
        public LanePosition Work { get; set; } = null;
        public LanePosition Destination { get; set; } = null;

        public Car()
        {
           
        }
    }
}
