using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Model.Tools
{
    //valamiert ide kellett irnom, h public, kulonban nem tudtam hasznalni a targetselectorban
    public interface IPlowTool
    {
        void ApplyEffect(LaneSegment segment);
        PlowToolType Type ();
    }
    public enum PlowToolType
    {
        Sweaper,
        Vomit,
        IceBreaker,
        Salt,
        Dragon
    }
}
