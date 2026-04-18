using UnityEngine;
using SnowPlow.Model.Map;
using UnityEngine.InputSystem.LowLevel;

namespace SnowPlow.Controller.Pathfinding
{
    public class CarTraversalPolicy : ITraversalPolicy
    {
        public bool CanEnterSegment(LanePosition position)
        {
            if (position == null) return false;

            //ha nincs karambol es nincs >=3 ho, akkor ra tud menni
            return !position.Lane[position.SegmentIndex].HasAccident && position.Lane[position.SegmentIndex].SnowLevel < 3;
        }

        //ha esetleg korlatozni akarjuk a visszafordulast, savvaltast, v egyebet
        public bool CanTransition(LanePosition from, LanePosition to)
        {
            if(from == null) return false;
            if(to == null) return false;
            return CanEnterSegment(to);
        }

        public float GetTraversalCost(LanePosition from, LanePosition to)
        {
            if (from == null || to == null) return float.PositiveInfinity;

            return 1f;
        }
    }
}
