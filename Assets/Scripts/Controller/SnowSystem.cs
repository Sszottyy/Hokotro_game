using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Controller
{
    public class SnowSystem
    {
        private List<Lane> Lanes { get; set;  } = new List<Lane>();

        public float SnowFallRate { get; set; } = 5f; // másodpercenkénti hóesés gyakorisága (kezdetben 5 másodperc)

        private float Dt { get; set; } = 0;
        public SnowSystem(List<Lane> lanes)
        {
            this.Lanes = lanes;
        }

        public void Update(float deltaTime)
        {
            Dt += deltaTime;

            while (Dt >= SnowFallRate)
            {
                Dt -= SnowFallRate;
                SnowFallRate = Math.Max(2f, SnowFallRate - 0.05f);

                foreach (var lane in Lanes)
                {
                    lane.AddSnow();
                }
            }


        }
    }
}
