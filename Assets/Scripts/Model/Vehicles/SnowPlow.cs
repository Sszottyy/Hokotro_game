using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;

namespace SnowPlow.Model.Vehicles
{
    public class SnowPlow : Vehicle
    {
        public IPlowTool EquippedTool { get; set; } = new SweaperTool(); // Default tool
        public event Action SnowCleared;

        public void ApplyToolEffect(LaneSegment segment)
        {
            EquippedTool?.ApplyEffect(segment);
            SnowCleared?.Invoke();
        }
        public SnowPlow() { }

        public SnowPlow(IPlowTool tool)
        {
            EquippedTool = tool;
        }
    }
}
