using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;

namespace SnowPlow.Model.Tools
{
    public class VomitTool : IPlowTool
    {
        public VomitTool() { }
        public void ApplyEffect(LanePosition pos)
        {
            pos.Lane[pos.SegmentIndex]?.RemoveAllSnow(); //ugyanaz maradt, csak most a lanepositionbol ki kell nyerni a szegmenst
        }
        public PlowToolType Type() => PlowToolType.Vomit;
    }
}
