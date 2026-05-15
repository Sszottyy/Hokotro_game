using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SnowPlow.Model.Map;

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
                    int i = 0;
                    foreach (var segment in lane.Segments)
                    {
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
                        i++;
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
    }
}
