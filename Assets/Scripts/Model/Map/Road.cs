using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace SnowPlow.Model.Map
{
    public class Road
    {
        public int Id { get; }
        public MapNode NodeA { get; }
        public MapNode NodeB { get; }

        private readonly List<Lane> lanesTowardsA;
        public IReadOnlyList<Lane> LanesTowardsA => lanesTowardsA;

        private readonly List<Lane> lanesTowardsB;
        public IReadOnlyList<Lane> LanesTowardsB => lanesTowardsB;

        public int SegmentCount { get; }

        internal Road(
            int id,
            MapNode nodeA,
            MapNode nodeB,
            int segmentCount,
            int laneCountTowardsA,
            int laneCountTowardsB)
        {
            if (nodeA == null) throw new ArgumentNullException(nameof(nodeA));

            if (nodeB == null) throw new ArgumentNullException(nameof(nodeB));

            if (nodeA == nodeB) throw new ArgumentException("Road endpoints cannot be the same.");

            if (segmentCount <= 0) throw new ArgumentOutOfRangeException(nameof(segmentCount), "Segment count must be greater than 0.");

            if (laneCountTowardsA < 0) throw new ArgumentOutOfRangeException(nameof(laneCountTowardsA), "Lane count cannot be negative.");

            if (laneCountTowardsB < 0) throw new ArgumentOutOfRangeException(nameof(laneCountTowardsB), "Lane count cannot be negative.");

            if (laneCountTowardsA == 0 && laneCountTowardsB == 0) throw new ArgumentException("A road must contain at least one lane.");

            Id = id;
            NodeA = nodeA;
            NodeB = nodeB;
            SegmentCount = segmentCount;

            lanesTowardsA = new(laneCountTowardsA);
            lanesTowardsB = new(laneCountTowardsB);

            int nextLaneId = 0;

            for (int i = 0; i < laneCountTowardsA; i++)
            {
                Lane lane = new(nextLaneId++, this, NodeB, NodeA, segmentCount);
                lanesTowardsA.Add(lane);
            }

            for (int i = 0; i < laneCountTowardsB; i++)
            {
                Lane lane = new(nextLaneId++, this, NodeA, NodeB, segmentCount);
                lanesTowardsB.Add(lane);
            }

            NodeA.AttachRoad(this);
            NodeB.AttachRoad(this);
        }

        public IReadOnlyList<Lane> GetAdjacentLanes(Lane lane)
        {
            if (lane == null) throw new ArgumentNullException(nameof(lane));

            if (lane.ParentRoad != this) throw new ArgumentException("The provided lane does not belong to this road.", nameof(lane));

            List<Lane> sameDirectionLanes;

            if (lane.StartNode == NodeB && lane.EndNode == NodeA)
            {
                sameDirectionLanes = lanesTowardsA;
            }
            else if (lane.StartNode == NodeA && lane.EndNode == NodeB)
            {
                sameDirectionLanes = lanesTowardsB;
            }
            else
            {
                throw new InvalidOperationException("Lane endpoints are inconsistent with the parent road.");
            }

            int index = sameDirectionLanes.IndexOf(lane);
            if (index < 0) throw new InvalidOperationException("Lane is not present in the expected lane collection.");

            List<Lane> result = new List<Lane>(2);

            if (index > 0)
            {
                result.Add(sameDirectionLanes[index - 1]);
            }

            if (index < sameDirectionLanes.Count - 1)
            {
                result.Add(sameDirectionLanes[index + 1]);
            }

            return result;
        }

        public bool Connects(MapNode first, MapNode second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            return (NodeA == first && NodeB == second) || (NodeA == second && NodeB == first);
        }

        public override string ToString()
        {
            return $"Road {Id}: {NodeA.Id} <-> {NodeB.Id}, lanes to A: {lanesTowardsA.Count}, lanes to B: {lanesTowardsB.Count}, segments: {SegmentCount}";
        }
    }
}
