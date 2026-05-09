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
            if (segment == null) return;
            if (EquippedTool == null) return;

            int snowBefore = segment.SnowLevel;
            bool iceBefore = segment.HasIce;
            int saltBefore = segment.SaltPower;

            EquippedTool.ApplyEffect(segment);

            bool changed =
                segment.SnowLevel != snowBefore ||
                segment.HasIce != iceBefore ||
                segment.SaltPower != saltBefore;

            if (changed)
            {
                SnowCleared?.Invoke();
            }
        }
        public SnowPlow() { }

        public override string ToString()
        {
            return "SnowPlow";
        }

        public SnowPlow(IPlowTool tool)
        {
            EquippedTool = tool;
        }
    }
}
