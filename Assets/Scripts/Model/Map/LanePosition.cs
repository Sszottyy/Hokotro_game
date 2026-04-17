using SnowPlow.Model.Map;
using System;
using UnityEngine;

public sealed class LanePosition : IEquatable<LanePosition>
{
    public Lane Lane { get; }
    public int SegmentIndex { get; }

    public LanePosition(Lane lane, int segmentIndex)
    {
        Lane = lane;
        SegmentIndex = segmentIndex;
    }
    public bool Equals(LanePosition other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return ReferenceEquals(Lane, other.Lane) && SegmentIndex == other.SegmentIndex;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as LanePosition);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Lane, SegmentIndex);
    }

    public static bool operator ==(LanePosition left, LanePosition right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(LanePosition left, LanePosition right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return $"{Lane} @ {SegmentIndex}";
    }
}
