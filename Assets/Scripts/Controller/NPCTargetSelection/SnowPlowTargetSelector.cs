using SnowPlow.Controller.Pathfinding;
using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;
using SnowPlow.Model.Vehicles;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace SnowPlow.Controller.NPCTargetSelection
{
    //fontos: mivel nem volt erre egyertelmu valasz, igy ugy keszult el, hogy a shopban 2 fajtat lehet vasarolni: hokotro es jegtoro fejut - ebbol keresi a legkozelebbit
    public static class SnowPlowTargetSelector
    {
        public static LanePosition SelectClosestTarget(LanePosition start, IPlowTool head, ITraversalPolicy policy)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            //BFS queue
            Queue<LanePosition> queue = new();

            // mar meglatogatott allapotok (ne menjunk korbe-korbe) - hashset, mert sokszor keresunk benne
            HashSet<LanePosition> visited = new();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                LanePosition current = queue.Dequeue();

                // ha ez mar egy megfelelo cel, keszen vagyunk - BFS miatt ez a legkozelebbi
                if (IsRelevantTarget(current, head))
                {
                    return current;
                }
                
                foreach (LanePosition neighbor in Pathfinder.GetNeighbors(current, policy))
                {
                    // ha mar jartunk itt, kihagyjuk
                    if (visited.Contains(neighbor)) continue;

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
            return start;
        }

        private static bool IsRelevantTarget(LanePosition current, IPlowTool head)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));

            LaneSegment segment = current.Lane[current.SegmentIndex];

            // fej tipus alapjan dontjuk el, hogy erdekes-e
            if (head is SweaperTool)
            {
                return segment.SnowLevel > 0;
            }
            // jegtoro fej -> jeget keres
            else if (head is IceBreaker)
            {
                return segment.HasIce;
            }
            return false;
        }
    }
}