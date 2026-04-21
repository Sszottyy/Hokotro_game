using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Controller
{
    public class SnowSystem
    {
        private List<Lane> lanes;

        public float snowFallRate = 0.2f; // egység / másodperc

        private float dt { get; set; } = 0;
        public SnowSystem(List<Lane> lanes)
        {
            this.lanes = lanes;
        }

        public void Update(float deltaTime)
        {
            dt += deltaTime;

            while (dt >= 5f)
            {
                dt -= 5f;

                foreach (var lane in lanes)
                {
                    lane.AddSnow();
                }
            }


        }
    }
}
