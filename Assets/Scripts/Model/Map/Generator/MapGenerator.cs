using SnowPlow.Model.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TreeEditor.TreeEditorHelper;

namespace SnowPlow.Model.Map.Generator
{
    public class MapData
    {
        public List<MapNode> Nodes { get; set; } = new List<MapNode>();
        public List<Road> Roads { get; set; } = new List<Road>();

        // Koordináták tárolása a vizualizációhoz/debughoz
        public Dictionary<MapNode, (int x, int y)> GridHints { get; set; } = new();
    }

    public class MapGenerator
    {
        private readonly System.Random _rng = new System.Random();

        /// <summary>
        /// Generál egy úthálózatot, ahol minden csomópont kereszteződés.
        /// </summary>
        /// <param name="intersectionCount">A generálandó kereszteződések száma.</param>
        public MapData Generate(int intersectionCount)
        {
            var data = new MapData();

            // --- 1. HORIZONTÁLIS RÁCS MÉRETEZÉSE ---
            // A szélesség legyen a négyzetgyök kb. 1.5-szöröse, a magasság pedig kevesebb
            int width = Mathf.CeilToInt(Mathf.Sqrt(intersectionCount) * 1.5f);
            int height = Mathf.CeilToInt((float)intersectionCount / width) + 1; // +1 biztonsági tartalék

            var grid = new MapNode[width, height];
            var gridCoords = new Dictionary<MapNode, (int x, int y)>();
            float skipProbability = 0.1f;

            // --- 2. NODE-OK LÉTREHOZÁSA (Sorfolytonosan a horizontális elnyúlásért) ---
            int nodeCounter = 0;

            // Y a külső ciklus -> Soronként haladunk (balról jobbra, fentről le)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (data.Nodes.Count >= intersectionCount) break;

                    // Biztonsági ellenőrzés: ha már csak annyi hely van, amennyi node kell, nem skippelünk
                    int remainingSlots = (width * height) - (y * width + x);
                    int neededNodes = intersectionCount - data.Nodes.Count;

                    if (_rng.NextDouble() < skipProbability && remainingSlots > neededNodes)
                        continue;

                    var node = new MapNode(nodeCounter);
                    nodeCounter++;

                    grid[x, y] = node;
                    gridCoords[node] = (x, y);
                    data.Nodes.Add(node);
                }
                if (data.Nodes.Count >= intersectionCount) break;
            }

            int roadId = 0;

            // --- 3. ALAP GRID ÉLEK (Figyelembe véve az új szélességet/magasságot) ---
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var current = grid[x, y];
                    if (current == null) continue;

                    // Jobbra szomszéd (X irány)
                    if (x + 1 < width && grid[x + 1, y] != null)
                        data.Roads.Add(CreateRoad(roadId++, current, grid[x + 1, y]));

                    // Alsó szomszéd (Y irány)
                    if (y + 1 < height && grid[x, y + 1] != null)
                        data.Roads.Add(CreateRoad(roadId++, current, grid[x, y + 1]));
                }
            }

            // --- 4. EXTRA (ÁTLÓS) UTAK ---
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

                // Csak akkor kötjük össze, ha nem egy vonalban vannak (átlós)
                if (ax == bx || ay == by) continue;

                int dx = Mathf.Abs(ax - bx);
                int dy = Mathf.Abs(ay - by);

                // Max 2 egység távolság az átlóban
                if (dx > 2 || dy > 2) continue;

                int dist = dx + dy;
                double chance = _rng.NextDouble();

                if ((dist == 2 && chance < 0.6) ||
                    (dist == 3 && chance < 0.25) ||
                    (dist == 4 && chance < 0.1))
                {
                    data.Roads.Add(CreateRoad(roadId++, a, b));
                    placed++;
                }
            }

            // --- 5. ÖSSZEFÜGGŐSÉG BIZTOSÍTÁSA ---
            EnsureConnectivity(data, gridCoords, ref roadId);

            data.GridHints = gridCoords;
            return data;
        }

        private Road CreateRoad(int id, MapNode a, MapNode b)
        {
            // Paraméterek véletlenszerűsítése (pl. sávok száma, szakaszok hossza)
            int segmentCount = _rng.Next(10, 25);
            int laneCountTowardsA = _rng.Next(1, 3);
            int laneCountTowardsB = _rng.Next(1, 3);

            return new Road(
                id: id,
                nodeA: a,
                nodeB: b,
                segmentCount: segmentCount,
                laneCountTowardsA: laneCountTowardsA,
                laneCountTowardsB: laneCountTowardsB
            );
        }

        private bool AreNodesConnected(MapNode a, MapNode b)
        {
            return a.ConnectedRoads.Any(r =>
                (r.NodeA == a && r.NodeB == b) ||
                (r.NodeA == b && r.NodeB == a));
        }

        private void EnsureConnectivity(MapData data, Dictionary<MapNode, (int x, int y)> gridCoords, ref int roadId)
        {
            var visited = new HashSet<MapNode>();
            var components = new List<List<MapNode>>();

            // Komponensek keresése (szigetek detektálása)
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
                        if (visited.Add(neighbor))
                            queue.Enqueue(neighbor);
                    }
                }
                components.Add(component);
            }

            if (components.Count <= 1) return;

            // Szigetek összekötése a legközelebbi szomszédos szigettel
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
                                bestDist = dist;
                                bestA = a;
                                bestB = b;
                                bestComponentIndex = i;
                            }
                        }
                    }
                }

                data.Roads.Add(CreateRoad(roadId++, bestA, bestB));
                mainComponent.AddRange(components[bestComponentIndex]);
                components.RemoveAt(bestComponentIndex);
            }
        }
    }
}