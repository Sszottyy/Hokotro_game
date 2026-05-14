using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Model.Tools
{
    public class IceBreaker : IPlowTool
    {
        public IceBreaker() { }
        public void ApplyEffect(LanePosition pos)
        {
            if (pos.Lane[pos.SegmentIndex] != null)
            {
                pos.Lane[pos.SegmentIndex]?.SetIce(false);
                pos.Lane[pos.SegmentIndex]?.AddSnow(); // the broken ice will create a layer of snow on the lane}

            }
        }
        
        public PlowToolType Type() => PlowToolType.IceBreaker;
    }
}