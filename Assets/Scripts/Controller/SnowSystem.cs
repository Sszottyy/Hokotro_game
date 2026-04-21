using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Controller
{
    public class SnowSystem
    {
        private List<Lane> lanes;

        public float snowFallRate = 5f; // másodpercenkénti hóesés gyakorisága (kezdetben 5 másodperc)

        private float dt { get; set; } = 0;
        public SnowSystem(List<Lane> lanes)
        {
            this.lanes = lanes;
        }

        public void Update(float deltaTime)
        {
            dt += deltaTime;

            while (dt >= snowFallRate)
            {
                dt -= snowFallRate;
                snowFallRate = Math.Max(2f, snowFallRate - 0.05f);

                foreach (var lane in lanes)
                {
                    lane.AddSnow();
                }
            }


        }
    }
}
