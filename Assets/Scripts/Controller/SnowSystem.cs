using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Controller
{
    public class SnowSystem
    {
        public List<Lane> Lanes { get; set;  } = new List<Lane>();

        public float SnowFallRate { get; set; } = 5f; // másodpercenkénti hóesés gyakorisága (kezdetben 5 másodperc)
        public float SnowChance { get; set; } = 0.3f;

        private float dt { get; set; } = 0;
        private float saltTimer { get; set; } = 0;
        public SnowSystem(List<Lane> lanes)
        {
            this.Lanes = lanes;
        }

        public void Update(float deltaTime)
        {
            dt += deltaTime;
            saltTimer += deltaTime;

            while (dt >= SnowFallRate)
            {
                dt -= SnowFallRate;
                SnowFallRate = Math.Max(2f, SnowFallRate - 0.05f);

                foreach (var lane in Lanes)
                {
                    foreach (var segment in lane.Segments)
                    {
                        if (new Random().NextDouble() < SnowChance)
                        {
                            segment.AddSnow();
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
    }
}
