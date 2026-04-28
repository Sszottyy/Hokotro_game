using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnowPlow.Model.Map
{
    public class Lane
    {
        public int Id { get; }
        public Road ParentRoad { get; }
        public MapNode StartNode { get; }
        public MapNode EndNode { get; }

        private readonly List<LaneSegment> segments;
        public IReadOnlyList<LaneSegment> Segments => segments;

        public LaneSegment this[int index] => segments[index];

        internal Lane(int id, Road parentRoad, MapNode startNode, MapNode endNode, int segmentCount)
        {
            if (parentRoad == null) throw new ArgumentNullException(nameof(parentRoad));

            if (startNode == null) throw new ArgumentNullException(nameof(startNode));

            if (endNode == null) throw new ArgumentNullException(nameof(endNode));

            if (startNode == endNode) throw new ArgumentException("Start node and end node cannot be the same.");

            if (segmentCount <= 0) throw new ArgumentOutOfRangeException(nameof(segmentCount), "Segment count must be greater than 0.");


            if (!((startNode == parentRoad.NodeA && endNode == parentRoad.NodeB) || (startNode == parentRoad.NodeB && endNode == parentRoad.NodeA)))
            {
                throw new ArgumentException("Lane endpoints must match the parent road endpoints.");
            }

            Id = id;
            ParentRoad = parentRoad;
            StartNode = startNode;
            EndNode = endNode;

            segments = new(segmentCount);
            for (int i = 0; i < segmentCount; i++)
            {
                segments.Add(new LaneSegment());
            }
        }

        public override string ToString()
        {
            return $"Lane {Id}: {StartNode.Id} -> {EndNode.Id}, segments: {segments.Count}";
        }
        //NEW! könyebb havat hozzáadni
        public void AddSnow(int amount)
        {
            foreach (var segment in segments)
            {
                segment.AddSnow(amount);
            }
        }
        public void AddSnow()
        {
            foreach (var segment in segments)
            {
                segment.AddSnow(1);
            }

        }
    }
}