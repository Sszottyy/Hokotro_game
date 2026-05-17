using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Text;
using SnowPlow.Model.Map;
using UnityEngine;

namespace SnowPlow.Controller
{
    public class SnowSystem
    {
        private readonly System.Random rng = new();
        public List<Lane> Lanes { get; set;  } = new List<Lane>();

        public float SnowFallRate { get; set; } = 5f; // másodpercenkénti hóesés gyakorisága (kezdetben 5 másodperc)
        public float SnowChance { get; set; } = 0.3f;

        private float dt { get; set; } = 0;
        private float saltTimer { get; set; } = 0;
        private SnowNetworkSync snowSync;
        public SnowSystem(List<Lane> lanes, SnowNetworkSync sync)
        {
            this.Lanes = lanes;
            snowSync = sync;
        }

        public void Update(float deltaTime)
        {
            return;//-------------------------------------------------------------
            dt += deltaTime;
            saltTimer += deltaTime;

            while (dt >= SnowFallRate)
            {
                dt -= SnowFallRate;
                SnowFallRate = Math.Max(2f, SnowFallRate - 0.05f);

                foreach (var lane in Lanes)
                {
                    for (int i = 0; i < lane.Segments.Count; i++)
                    {
                        LaneSegment segment = lane.Segments[i];
                        if (i < 4 || i >= lane.Segments.Count - 4)
                        {
                            continue;
                        }
                        if (rng.NextDouble() < SnowChance)
                        {
                            segment.AddSnow();

                            snowSync.UpdateSnowClientRpc(
                                lane.ParentRoad.Id,
                                lane.Id,
                                i,
                                segment.SnowLevel,
                                segment.HasIce,
                                 segment.SaltPower
                            );
                        }
                        
                    }
                }
                if (SnowChance < 0.8f)
                {
                    SnowChance += 0.01f;
                }
            }
            while( saltTimer >= 3f)
            {
                saltTimer -= 3f;
                foreach (var lane in Lanes)
                {
                    lane.UpdateSaltPower();
                }
            }


        }
        public void GenerateInitialSnow()
        {
            Debug.Log("[SNOW] GenerateInitialSnow CALLED");
            foreach (var lane in Lanes)
            {
                for (int i = 0; i < lane.Segments.Count; i++)
                {
                    LaneSegment segment = lane.Segments[i];

                    if (rng.NextDouble() < 0.35f)
                    {
                        int amount = rng.Next(1, 4);

                        segment.AddSnow(amount);

                        snowSync.UpdateSnowClientRpc(
                            lane.ParentRoad.Id,
                            lane.Id,
                            i,
                            segment.SnowLevel,
                            segment.HasIce,
                            segment.SaltPower
                        );
                    }
                }
            }
        }
    }
}
