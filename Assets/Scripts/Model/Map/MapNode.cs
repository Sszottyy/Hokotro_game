using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace SnowPlow.Model.Map
{
    public class MapNode
    {
        //NEW - removed NodeType
        public int Id { get; }

        private readonly List<Road> connectedRoads;
        public IReadOnlyList<Road> ConnectedRoads => connectedRoads;

        internal MapNode(int id)
        {
            Id = id;
            connectedRoads = new();
        }

        internal void AttachRoad(Road road)
        {
            if (road == null) throw new ArgumentNullException(nameof(road));

            if (road.NodeA != this && road.NodeB != this) throw new ArgumentException("Road is not connected to this node.", nameof(road));

            if (!connectedRoads.Contains(road)) connectedRoads.Add(road);
        }

        public override string ToString()
        {
            return $"Node {Id}";
        }
    }
}