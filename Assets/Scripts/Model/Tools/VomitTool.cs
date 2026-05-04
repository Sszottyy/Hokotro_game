using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;

namespace SnowPlow.Model.Tools
{
    class VomitTool : IPlowTool
    {
        public VomitTool() { }
        public void ApplyEffect(LaneSegment segment)
        {
            segment.RemoveAllSnow();
        }
        public PlowToolType Type() => PlowToolType.Vomit;
    }
}
