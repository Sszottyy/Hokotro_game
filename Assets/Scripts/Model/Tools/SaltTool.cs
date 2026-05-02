using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;

namespace SnowPlow.Model.Tools
{
    class SaltTool : IPlowTool
    {
        private int fuel = 0;
        public int Fuel { get; private set; }

        public SaltTool(int fuel)
        {
            Fuel = fuel;
        }

        public SaltTool() { }

        public void AddSalt(int ammount)
        {
            Fuel += ammount;
        }
        public void ApplyEffect(LaneSegment laneSegment)
        {

            if (laneSegment != null && Fuel > 0)
            {
                laneSegment.AddSaltPower(1);
                Fuel -= 1;
            }
        }

        public PlowToolType Type() => PlowToolType.Salt;
    }
}
