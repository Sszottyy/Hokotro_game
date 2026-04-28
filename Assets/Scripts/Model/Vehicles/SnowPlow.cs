using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;

namespace SnowPlow.Model.Vehicles
{
    class SnowPlow : Vehicle
    {
        public IPlowTool EquippedTool { get; set; } = new SweaperTool(); // Default tool

        public void ApplyToolEffect(LaneSegment segment)
        {
            EquippedTool?.ApplyEffect(segment);
        }
        public SnowPlow() { }

        public SnowPlow(IPlowTool tool)
        {
            EquippedTool = tool;
        }
    }
}
