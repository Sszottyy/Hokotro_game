using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;

namespace SnowPlow.Model.Tools
{
    public class DragonTool : IPlowTool
    {
        private int fuel = 0;
        public int Fuel { get; private set; }

        public DragonTool() { }
        public DragonTool(int fuel)
        {
            Fuel = fuel;
        }
        public void ApplyEffect(LaneSegment laneSegment)
        {
            if (laneSegment != null && Fuel > 0)
            {
                laneSegment.RemoveAllSnow();
                Fuel -= 1;
            }
        }

        public void AddFuel(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(" fuelamount should be bigger than 0");
            Fuel += amount;
        }

        public PlowToolType Type() => PlowToolType.Dragon;
    }
}
