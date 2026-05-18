using SnowPlow.Model.Map;
using SnowPlow.Model.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.HableCurve;

namespace SnowPlow.Model.Vehicles
{
    public class SnowPlow : Vehicle
    {
        public IPlowTool EquippedTool { get; set; } = new SweaperTool(); // Default tool
        public event Action SnowCleared;
        public PlowToolType EquippedToolType;

        //adjuted to LanePosition
        public void ApplyToolEffect(LanePosition pos)
                {
                    Debug.Log(
            $"[OWNER CHECK] " +
            $"VehicleOwner={Owner?.Name} | " +
            $"Team={Owner?.Team?.Name}"
        );
                    Debug.Log(
            $"[LOCAL PLAYER] " +
            $"{GameManager.Instance.LocalPlayer?.Name} | " +
            $"{GameManager.Instance.LocalPlayer?.Team?.Name}"
        );
            if (!NetworkManager.Singleton.IsServer)
                return;
            //Debug.Log("[PLOW] ApplyToolEffect CALLED");
            if (pos == null) return;
            if (pos.Lane == null) return;
            if (EquippedTool == null) return;

            int snowBefore = pos.Lane[pos.SegmentIndex].SnowLevel;
            bool iceBefore = pos.Lane[pos.SegmentIndex].HasIce;
            int saltBefore = pos.Lane[pos.SegmentIndex].SaltPower;
            //Debug.Log($"[PLOW] snowBefore = {snowBefore}");
            /*Debug.Log(
    $"[PLOW] Owner = {Owner?.Name}, Team = {Owner?.Team?.Name}"
);*/
            EquippedTool.ApplyEffect(pos);

            bool changed =
                pos.Lane[pos.SegmentIndex].SnowLevel != snowBefore ||
                pos.Lane[pos.SegmentIndex].HasIce != iceBefore ||
                pos.Lane[pos.SegmentIndex].SaltPower != saltBefore;

            if (changed)
            {
                SnowCleared?.Invoke();

                int reward = snowBefore * 5;

                if (Owner != null && Owner.Team != null)
                {
                    Owner.Team.AddMoney(reward);
                    Debug.Log($"[MONEY] Added {reward}");

                    Debug.Log(
                        $"[MONEY] {Owner.Name} earned {reward}. Team money: {Owner.Team.Money}"
                    );
                }
            }

            Debug.Log("APPLY snowPlow instance: " + GetHashCode());
            Debug.Log("APPLY equipped class: " + EquippedTool.GetType().Name);
            Debug.Log("APPLY equipped enum: " + EquippedTool.Type());
            var sync =
    UnityEngine.Object.FindObjectOfType<SnowNetworkSync>();

            if (sync != null)
            {
                LaneSegment segment =
                    pos.Lane[pos.SegmentIndex];

                sync.UpdateSnowClientRpc(
                    pos.Lane.ParentRoad.Id,
                    pos.Lane.Id,
                    pos.SegmentIndex,
                    segment.SnowLevel,
                    segment.HasIce,
                    segment.SaltPower);
            }
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
