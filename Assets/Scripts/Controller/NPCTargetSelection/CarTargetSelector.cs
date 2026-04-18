using SnowPlow.Controller.Pathfinding;
using SnowPlow.Model.Map;
using SnowPlow.Model.Map.Generator;
using SnowPlow.Model.Tools;
using SnowPlow.Model.Vehicles;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Random = System.Random;

namespace SnowPlow.Controller.NPCTargetSelection
{
    // ez a class az auto fix pontjait kezeli:
    // - ha nincs home/work, kioszt neki randomot
    // - ha nincs destination, beallit egy kezdo celt
    // - ha elerte a celt, visszavalt a masik fix pontra
    public static class CarTargetSelector
    {
        private static readonly Random _rng = new Random();

        public static void UpdateTargets(Car car, MapData map)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            if (map == null) throw new ArgumentNullException(nameof(map));

            // osszes lehetseges celpont (minden lane minden segmentje)
            List<LanePosition> candidates = CollectAllLanePositions(map);

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("There are no available lane positions in the map.");
            }

            // ha nincs home, kap egy random poziciot
            if (car.Home == null)
            {
                car.Home = GetRandomPosition(candidates);
            }

            // ha nincs work, kap egy random poziciot, ami nem ugyanaz mint a home
            if (car.Work == null)
            {
                LanePosition workCandidate;

                do
                {
                    workCandidate = GetRandomPosition(candidates);
                }
                while (workCandidate.Equals(car.Home));

                car.Work = workCandidate;
            }

            // ha nincs destination, akkor kap egy kezdo celt
            // itt most ugy indul, hogy menjen a work fele
            if (car.Destination == null)
            {
                car.Destination = car.Work;
                return;
            }

            // ha elerte a jelenlegi celjat, valtson a masik fix pontra
            if (HasReachedDestination(car))
            {
                if (car.Destination.Equals(car.Home))
                {
                    car.Destination = car.Work;
                }
                else if (car.Destination.Equals(car.Work))
                {
                    car.Destination = car.Home;
                }
                else
                {
                    // ha valamiert a destination nem home es nem work,
                    // akkor fallbackkent menjen a work fele
                    car.Destination = car.Work;
                }
            }
        }

        private static bool HasReachedDestination(Car car)
        {
            if (car.CurrentPosition == null) throw new ArgumentNullException(nameof(car.CurrentPosition));
            if (car.Destination == null) return false;

            // akkor tekintjuk megerkezettnek, ha pontosan ugyanazon a lanePosition-on van
            return car.CurrentPosition.Equals(car.Destination);
        }

        private static LanePosition GetRandomPosition(List<LanePosition> candidates)
        {
            int index = _rng.Next(candidates.Count);
            return candidates[index];
        }

        private static List<LanePosition> CollectAllLanePositions(MapData map)
        {
            List<LanePosition> result = new();

            // minden roadon vegigmegyunk
            foreach (Road road in map.Roads)
            {
                // mindket iranyu lane-eket bejarjuk
                foreach (Lane lane in road.LanesTowardsA)
                {
                    AddLanePositions(lane, result);
                }

                foreach (Lane lane in road.LanesTowardsB)
                {
                    AddLanePositions(lane, result);
                }
            }

            return result;
        }

        private static void AddLanePositions(Lane lane, List<LanePosition> result)
        {
            for (int i = 0; i < lane.Segments.Count; i++)
            {
                result.Add(new LanePosition(lane, i));
            }
        }
    }
}