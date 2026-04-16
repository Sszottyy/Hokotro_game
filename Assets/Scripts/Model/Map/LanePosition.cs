using SnowPlow.Model.Map;
using UnityEngine;

public sealed class LanePosition
{
    public Lane Lane { get; }
    public int SegmentIndex { get; }

    public LanePosition(Lane lane, int segmentIndex)
    {
        Lane = lane;
        SegmentIndex = segmentIndex;
    }

    public override string ToString()
    {
        return $"{Lane} @ {SegmentIndex}";
    }
}
