using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;

namespace SnowPlow.Model.Vehicles
{
    class SnowPlow : Vehicle
    {
        public IPlowTool equippedTool { get; set; } = new SweaperTool(); // Default tool

        public void ApplyToolEffect(LaneSegment segment)
        {
            equippedTool?.ApplyEffect(segment);
        }
        public SnowPlow() { }

        public SnowPlow(IPlowTool tool)
        {
            equippedTool = tool;
        }
    }
}
