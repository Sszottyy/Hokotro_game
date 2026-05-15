using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Model.Map;

namespace SnowPlow.Model.Tools
{
    public class SweaperTool : IPlowTool
    {
        public SweaperTool() { }

        public void ApplyEffect(LanePosition pos)
        {
            Lane sourceLane = pos.Lane;
            int segmentIndex = pos.SegmentIndex;

            LaneSegment sourceSegment = sourceLane[segmentIndex];

            int snowToMove = sourceSegment.SnowLevel;

            if (snowToMove <= 0)
            {
                return;
            }

            sourceSegment?.RemoveAllSnow();

            // Megkeressük a jobb oldali sávot.
            Lane rightLane = GetRightLane(sourceLane);

            if (rightLane == null)
            {
                // Nincs jobb oldali sáv, tehát a söprő letolja a havat az útról.
                return;
            }

            if (segmentIndex >= rightLane.Segments.Count)
            {
                return;
            }

            LaneSegment targetSegment = rightLane[segmentIndex];

            // A modell szabályait követjük: AddSnow kezeli a sózott/jeges eseteket.
            targetSegment.AddSnow(snowToMove);
            var sync =
    UnityEngine.Object.FindObjectOfType<SnowNetworkSync>();

            if (sync != null)
            {
                sync.UpdateSnowClientRpc(
                    sourceLane.ParentRoad.Id,
                    sourceLane.Id,
                    segmentIndex,
                    sourceSegment.SnowLevel,
                    sourceSegment.HasIce,
                    sourceSegment.SaltPower);

                sync.UpdateSnowClientRpc(
                    rightLane.ParentRoad.Id,
                    rightLane.Id,
                    segmentIndex,
                    targetSegment.SnowLevel,
                    targetSegment.HasIce,
                    targetSegment.SaltPower);
            }
            if (MapVisualizer.Instance.SegmentDirectory.TryGetValue(
                    targetSegment,
                    out VisualSegment targetVisual))
            {
                targetVisual.UpdateVisuals();
            }

            if (MapVisualizer.Instance.SegmentDirectory.TryGetValue(
                    sourceSegment,
                    out VisualSegment sourceVisual))
            {
                sourceVisual.UpdateVisuals();
            }
        }

        private Lane GetRightLane(Lane lane)
        {
            if (lane == null) return null;
            if (lane.ParentRoad == null) return null;

            Road road = lane.ParentRoad;

            IReadOnlyList<Lane> sameDirectionLanes;

            if (lane.StartNode == road.NodeB && lane.EndNode == road.NodeA)
            {
                sameDirectionLanes = road.LanesTowardsA;
            }
            else if (lane.StartNode == road.NodeA && lane.EndNode == road.NodeB)
            {
                sameDirectionLanes = road.LanesTowardsB;
            }
            else
            {
                return null;
            }

            int laneIndex = -1;

            for (int i = 0; i < sameDirectionLanes.Count; i++)
            {
                if (ReferenceEquals(sameDirectionLanes[i], lane))
                {
                    laneIndex = i;
                    break;
                }
            }

            if (laneIndex < 0)
            {
                return null;
            }

            // Konvenció: azonos irányú lane-listában index + 1 = jobb oldali sáv.
            int rightLaneIndex = laneIndex + 1;

            if (rightLaneIndex >= sameDirectionLanes.Count)
            {
                return null;
            }

            return sameDirectionLanes[rightLaneIndex];
        }

        public PlowToolType Type() => PlowToolType.Sweaper;
    }
}
