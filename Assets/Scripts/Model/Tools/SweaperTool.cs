using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Model.Tools
{
    class SweaperTool : IPlowTool
    {
        public SweaperTool() { }
        public void ApplyEffect(LaneSegment segment)
        {
            segment?.RemoveAllSnow();
        }
        public PlowToolType Type() => PlowToolType.Sweaper;
    }
}
