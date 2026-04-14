using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;

namespace SnowPlow.Model.Vehicles
{
    class SnowPlow : Vehicle
    {
        public IPlowTool equippedTool { get; set; }

        public void ApplyToolEffect(LaneSegment segment)
        {
            equippedTool?.ApplyEffect(segment);
        }
        public SnowPlow()
        {
            IPlowTool equippedTool = new SweaperTool(); // Default tool
        }
    }
}
