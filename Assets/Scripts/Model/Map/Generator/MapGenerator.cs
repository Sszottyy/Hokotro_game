using SnowPlow.Model.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SnowPlow.Model.Map.Generator
{
    public class MapData
    {
        public List<MapNode> Nodes { get; set; } = new List<MapNode>();
        public List<Road> Roads { get; set; } = new List<Road>();
        public Dictionary<MapNode, (float x, float y)> GridHints { get; set; } = new();
    }

    public class MapGenerator
    {
        private readonly System.Random _rng;

        // Hány szegmens jusson 1 logikai rácsegységre?
        // Ha a Unity-ben a gridSpacing = 20, akkor ezzel 2.0 vizuális méretű lesz 1 szegmens!
        private const int SegmentsPerGridUnit = 10;

        public MapGenerator(int seed)
        {
            _rng = new System.Random(seed);
        }

        public MapData Generate(int intersectionCount)
        {
            var data = new MapData();

            int width = Mathf.CeilToInt(Mathf.Sqrt(intersectionCount) * 1.5f);
            int height = Mathf.CeilToInt((float)intersectionCount / width) + 1;

            var grid = new MapNode[width, height];
            var gridCoords = new Dictionary<MapNode, (float x, float y)>();
            float skipProbability = 0.1f;

            int nodeCounter = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (data.Nodes.Count >= intersectionCount) break;

                    int remainingSlots = (width * height) - (y * width + x);
                    int neededNodes = intersectionCount - data.Nodes.Count;

                    if (_rng.NextDouble() < skipProbability && remainingSlots > neededNodes)
                        continue;

                    var node = new MapNode(nodeCounter);
                    nodeCounter++;

                    grid[x, y] = node;

                    // Organikus rács: pici seedelt eltolás a generátorban
                    float jitterX = nodeCounter == 1 ? 0 : (float)(_rng.NextDouble() * 0.8 - 0.4);
                    float jitterY = nodeCounter == 1 ? 0 : (float)(_rng.NextDouble() * 0.8 - 0.4);

                    gridCoords[node] = (x + jitterX, y + jitterY);
                    data.Nodes.Add(node);
                }
                if (data.Nodes.Count >= intersectionCount) break;
            }

            int roadId = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var current = grid[x, y];
                    if (current == null) continue;

                    if (x + 1 < width && grid[x + 1, y] != null)
                        data.Roads.Add(CreateRoad(roadId++, current, grid[x + 1, y], gridCoords));

                    if (y + 1 < height && grid[x, y + 1] != null)
                        data.Roads.Add(CreateRoad(roadId++, current, grid[x, y + 1], gridCoords));
                }
            }

            int nodeCount = data.Nodes.Count;
            int extraRoads = Mathf.Max(1, nodeCount / 8);
            int placed = 0;
            int attempts = 0;

            while (placed < extraRoads && attempts < extraRoads * 6)
            {
                attempts++;
                var a = data.Nodes[_rng.Next(nodeCount)];
                var b = data.Nodes[_rng.Next(nodeCount)];

                if (a == b || AreNodesConnected(a, b)) continue;

                var (ax, ay) = gridCoords[a];
                var (bx, by) = gridCoords[b];

                // Int helyett most már float távolság az if-ben
                int dx = Mathf.RoundToInt(Mathf.Abs(ax - bx));
                int dy = Mathf.RoundToInt(Mathf.Abs(ay - by));

                if (dx == 0 || dy == 0 || dx > 2 || dy > 2) continue;

                int dist = dx + dy;
                double chance = _rng.NextDouble();

                if ((dist == 2 && chance < 0.6) || (dist == 3 && chance < 0.25) || (dist == 4 && chance < 0.1))
                {
                    data.Roads.Add(CreateRoad(roadId++, a, b, gridCoords));
                    placed++;
                }
            }

            EnsureConnectivity(data, gridCoords, ref roadId);
            data.GridHints = gridCoords;

            return data;
        }

        // AZ ÚJ SZÁMÍTÁS: Pitagorasz-tétel alapján mérjük a hosszt, és kerekítjük!
        private Road CreateRoad(int id, MapNode a, MapNode b, Dictionary<MapNode, (float x, float y)> gridCoords)
        {
            var posA = gridCoords[a];
            var posB = gridCoords[b];

            float dx = posA.x - posB.x;
            float dy = posA.y - posB.y;
            float gridDistance = (float)Math.Sqrt(dx * dx + dy * dy);

            // Itt lőjük be, hogy minden logikai út tökéletes darabszámú szegmensből álljon:
            int segmentCount = Math.Max(1, (int)Math.Round(gridDistance * SegmentsPerGridUnit));

            int laneCountTowardsA = _rng.Next(1, 3);
            int laneCountTowardsB = _rng.Next(1, 3);

            return new Road(id, a, b, segmentCount, laneCountTowardsA, laneCountTowardsB);
        }

        private bool AreNodesConnected(MapNode a, MapNode b)
        {
            return a.ConnectedRoads.Any(r => (r.NodeA == a && r.NodeB == b) || (r.NodeA == b && r.NodeB == a));
        }

        private void EnsureConnectivity(MapData data, Dictionary<MapNode, (float x, float y)> gridCoords, ref int roadId)
        {
            var visited = new HashSet<MapNode>();
            var components = new List<List<MapNode>>();

            foreach (var startNode in data.Nodes)
            {
                if (visited.Contains(startNode)) continue;

                var component = new List<MapNode>();
                var queue = new Queue<MapNode>();
                queue.Enqueue(startNode);
                visited.Add(startNode);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    component.Add(current);
                    foreach (var road in current.ConnectedRoads)
                    {
                        var neighbor = road.NodeA == current ? road.NodeB : road.NodeA;
                        if (visited.Add(neighbor)) queue.Enqueue(neighbor);
                    }
                }
                components.Add(component);
            }

            if (components.Count <= 1) return;

            while (components.Count > 1)
            {
                float bestDist = float.MaxValue;
                MapNode bestA = null, bestB = null;
                int bestComponentIndex = -1;
                var mainComponent = components[0];

                for (int i = 1; i < components.Count; i++)
                {
                    foreach (var a in mainComponent)
                    {
                        foreach (var b in components[i])
                        {
                            var (ax, ay) = gridCoords[a];
                            var (bx, by) = gridCoords[b];
                            float dist = Mathf.Abs(ax - bx) + Mathf.Abs(ay - by);

                            if (dist < bestDist)
                            {
                                bestDist = dist; bestA = a; bestB = b; bestComponentIndex = i;
                            }
                        }
                    }
                }

                data.Roads.Add(CreateRoad(roadId++, bestA, bestB, gridCoords));
                mainComponent.AddRange(components[bestComponentIndex]);
                components.RemoveAt(bestComponentIndex);
            }
        }
    }
}