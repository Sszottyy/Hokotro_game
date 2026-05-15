using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.HableCurve;

namespace SnowPlow.Model.Vehicles
{
    public class SnowPlow : Vehicle
    {
        public IPlowTool EquippedTool { get; set; } = new SweaperTool(); // Default tool
        public event Action SnowCleared;

        //adjuted to LanePosition
        public void ApplyToolEffect(LanePosition pos)
        {
            if (pos == null) return;
            if (pos.Lane == null) return;
            if (EquippedTool == null) return;

            int snowBefore = pos.Lane[pos.SegmentIndex].SnowLevel;
            bool iceBefore = pos.Lane[pos.SegmentIndex].HasIce;
            int saltBefore = pos.Lane[pos.SegmentIndex].SaltPower;

            EquippedTool.ApplyEffect(pos);

            bool changed =
                pos.Lane[pos.SegmentIndex].SnowLevel != snowBefore ||
                pos.Lane[pos.SegmentIndex].HasIce != iceBefore ||
                pos.Lane[pos.SegmentIndex].SaltPower != saltBefore;

            if (changed)
            {
                SnowCleared?.Invoke();
            }

            Debug.Log("APPLY snowPlow instance: " + GetHashCode());
            Debug.Log("APPLY equipped class: " + EquippedTool.GetType().Name);
            Debug.Log("APPLY equipped enum: " + EquippedTool.Type());
        }
        public SnowPlow() { }

        public override string ToString()
        {
            return "SnowPlow";
        }

        public SnowPlow(IPlowTool tool)
        {
            EquippedTool = tool;
        }
    }
}
