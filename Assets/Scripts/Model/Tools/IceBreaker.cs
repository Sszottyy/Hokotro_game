using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Model.Tools
{
    class IceBreaker : IPlowTool
    {
        public IceBreaker() { }
        public void ApplyEffect(LaneSegment segment)
        {
            segment.SetIce(false);
            segment.AddSnow(); // the broken ice will create a layer of snow on the lane
        }
    }
}
