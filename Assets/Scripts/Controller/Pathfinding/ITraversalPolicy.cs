using UnityEngine;
using SnowPlow.Model.Map;

namespace SnowPlow.Controller.Pathfinding
{
    public interface ITraversalPolicy
    {
        bool CanEnterSegment(LanePosition position);
        bool CanTransition(LanePosition from, LanePosition to);
        float GetTraversalCost(LanePosition from, LanePosition to);
    }
}
