using SnowPlow.Model.Map;
using SnowPlow.Model.Vehicles;
using System;
using UnityEngine;

public class Bus : Vehicle
{
    public int CompletedTrips { get; private set; }
    public int TotalPassangers { get; private set; }
    public LaneSegment StationA { get; set; }
    public LaneSegment StationB { get; set; }

    public event Action<int> TripCompleted;
    public event Action<int> PassengersDroppedOff;

    public Bus()
    {
        CompletedTrips = 0;
    }

    public override string ToString()
    {
        return "Bus";
    }
    public void IncreaseTripCount(float elapsedSeconds)
    {
        CompletedTrips++;

        // 40s = 25 pts, faster = more, slower = less, floor at 1
        const float targetTime = 40f;
        const float targetPoints = 25f;
        int score = Mathf.Max(1, Mathf.RoundToInt(targetPoints * targetTime / elapsedSeconds));

        TripCompleted?.Invoke(score);
    }

    public void IncreasePassangers(int droppedOffPassangers)
    {
        TotalPassangers += droppedOffPassangers;
        PassengersDroppedOff?.Invoke(droppedOffPassangers);
    }

    public void BusOnSegment(LaneSegment segment)
    {
        if (segment.SnowLevel >= 10)
        {
            this.isBlocked = true;
        }
    }
}