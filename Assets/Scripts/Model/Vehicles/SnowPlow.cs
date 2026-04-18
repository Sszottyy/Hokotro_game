using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;

namespace SnowPlow.Model.Vehicles
{
    class SnowPlow : Vehicle
    {
        public IPlowTool EquippedTool { get; set; }

        public void ApplyToolEffect(LaneSegment segment)
        {
            EquippedTool?.ApplyEffect(segment);
        }
        public SnowPlow()
        {
            IPlowTool EquippedTool = new SweaperTool(); // Default tool
        }
    }
}
