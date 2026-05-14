using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;

namespace SnowPlow.Model.Tools
{
    public class SaltTool : IPlowTool
    {
        private int fuel = 0;
        public int Fuel { get; private set; }

        public SaltTool(int fuel)
        {
            Fuel = fuel;
        }

        public SaltTool() { }

        public void AddFuel(int amount) //atneveztem, kicsit felrevezetonek gondoltam
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("Salt amount should be bigger than 0");
            Fuel += amount;
        }
        public void ApplyEffect(LanePosition pos)
        {

            if (pos.Lane[pos.SegmentIndex] != null && Fuel > 0)
            {
                pos.Lane[pos.SegmentIndex].AddSaltPower(1); //ugyanaz maradt, csak most a lanepositionbol ki kell nyerni a szegmenst
                Fuel -= 1;
            }
        }

        public PlowToolType Type() => PlowToolType.Salt;
    }
}
