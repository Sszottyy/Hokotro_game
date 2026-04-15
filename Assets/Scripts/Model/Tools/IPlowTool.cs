using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Model.Tools
{
    interface IPlowTool
    {
        void ApplyEffect(LaneSegment segment);
    }
}
