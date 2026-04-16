using UnityEngine;
using System.Collections.Generic;
using SnowPlow.Model.Map;
using System;

namespace SnowPlow.Controller.Pathfinding
{
    public class Pathfinder
    {
        public List<LanePosition> FindPath(LanePosition start, LanePosition end, ITraversalPolicy policy)
        {
            if(start == null) throw new ArgumentNullException(nameof(start));
            if(end == null) throw new ArgumentNullException(nameof(end));
            if(policy == null) throw new ArgumentNullException(nameof(policy));

            //mi tortenik, ha blokkolt a celja?
            if (!policy.CanEnterSegment(end)) return new List<LanePosition>();

            //openSet - azon allapotok halmaza, amit meg meg akarunk viszgalni
            //closedSet - allapotok, amiket mar feldolgoztunk (azert hashSet, mert rendkivul sokszor be kell jarni kesobb)
            List<LanePosition> openSet = new() { start };
            HashSet<LanePosition> closedSet = new();

            Dictionary<LanePosition, LanePosition> cameFrom = new(); //ut visszafejtesehez
            Dictionary<LanePosition, float> gScore = new() // eddig mennyibe kerult eljutni az adott allapotba (mivel nincsenek sulyok egesz)
            {
                [start] = 0f
            };
            Dictionary<LanePosition, float> fScore = new() // A* becsult teljes koltseg (0, ezert valojaban Dijkstra-szeru viselkedese lesz)
            {
                [start] = Heuristic(start, end)
            };

            while (openSet.Count > 0)
            {
                LanePosition current = GetLowestFScore(openSet, fScore);

                //ha az aktualis a cel, akkor kesz, vissza lehet fejteni
                if (current.Equals(end)) return ReconstructPath(cameFrom, current);

                //atvizsgaljuk -> atkerul a feldolgozottak koze
                openSet.Remove(current);
                closedSet.Add(current);

                foreach (LanePosition neighbor in GetNeighbors(current, policy))
                {
                    if (closedSet.Contains(neighbor)) continue; //ha mar atvizsgaltuk, akkor kihagyjuk

                    float cumulativeGScore = gScore[current] + policy.GetTraversalCost(current, neighbor);

                    //ha nincs benne, felvesszuk a vizsgalandok koze
                    //ha benne van, de nem jobb az ut, nem frissitunk
                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                    else if (gScore.TryGetValue(neighbor, out float existingGScore) && cumulativeGScore >= existingGScore) continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = cumulativeGScore;
                    fScore[neighbor] = cumulativeGScore + Heuristic(neighbor, end);
                }
            }
            return new List<LanePosition>();
        }

        private IEnumerable<LanePosition> GetNeighbors(LanePosition current, ITraversalPolicy policy)
        {
            List<LanePosition> neighbors = new();
            Lane currentLane = current.Lane; //melyik sav
            int currentIndex = current.SegmentIndex; //savon belul melyik szegmens
            int lastIndex = currentLane.Segments.Count - 1; //savban mennyi szegmens

            //ha nem a lane utolso segmentjen van (tehat nem kell keresztezodesen keresztul valasztani)
            if (currentIndex < lastIndex)
            {
                LanePosition forward = new(currentLane, currentIndex + 1);
                if (policy.CanTransition(current, forward)) neighbors.Add(forward); //ha nem blokkolt, mehet a szomszedok koze
            }

            //savvaltas (a GetAdjacentLanes biztosra megy, hogy csak tenylegesen szomszedos savokat adjon vissza)
            IReadOnlyList<Lane> adjancentLanes = currentLane.ParentRoad.GetAdjacentLanes(currentLane);
            if (adjancentLanes.Count > 0)
            {
                foreach (Lane adjacentLane in adjancentLanes)
                {
                    if (currentIndex < adjacentLane.Segments.Count) //letezik-e ugyanaz a szegmens a szomsedosban (kell leteznie)
                    {
                        LanePosition laneChange = new(adjacentLane, currentIndex);

                        if (policy.CanTransition(current, laneChange)) neighbors.Add(laneChange);//ha nem blokkolt, mehet a szomszedok koze
                    }
                }
            }

            //ha a sav utolso szegmensen vagyunk -> keresztezodesbol uj ut valasztasa
            if (currentIndex == lastIndex)
            {
                MapNode node = currentLane.EndNode; //keresztezodes, amihez erkezett

                foreach(Lane outgoing in GetOutGoingLanes(node))
                {
                    LanePosition nextLaneStart = new(outgoing, 0);

                    if(policy.CanTransition(current,nextLaneStart)) neighbors.Add(nextLaneStart); // ha ra szabad menni, akkor hozzaadjuk
                }
            }
            return neighbors;
        }

        private IEnumerable<Lane> GetOutGoingLanes(MapNode node)
        {
            List<Lane> result = new();

            foreach (Road road in node.ConnectedRoads) 
            {
                foreach(Lane lane in road.LanesTowardsA)
                {
                    if (lane.StartNode == node)result.Add(lane);
                }
                foreach (Lane lane in road.LanesTowardsB)
                {
                    if (lane.StartNode == node) result.Add(lane);
                }
            }
            return result;
        }

        private LanePosition GetLowestFScore(List<LanePosition> openSet, Dictionary<LanePosition, float> fScore)
        {
            LanePosition best = openSet[0];
            float bestScore = GetScore(best, fScore);

            for (int i = 1; i < openSet.Count; i++) { 
                LanePosition candidate = openSet[i];
                float candidateScore = GetScore(candidate, fScore);
                if (candidateScore < bestScore) { best = candidate; bestScore = candidateScore; }
            }
            return best;
        }

        private float GetScore(LanePosition pos, Dictionary<LanePosition, float> scores)
        {
            if(scores.TryGetValue(pos, out float score)) return score;

            return float.PositiveInfinity;
        }

        private List<LanePosition> ReconstructPath(Dictionary<LanePosition, LanePosition> cameFrom, LanePosition current)
        {
            List<LanePosition> path = new List<LanePosition>() { current };

            while(cameFrom.TryGetValue(current, out LanePosition prev))
            {
                current = prev;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private float Heuristic(LanePosition start, LanePosition end) {
            return 0f;
        }
    }
}