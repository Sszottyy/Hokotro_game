using SnowPlow.Model.Map;
using SnowPlow.Model.Vehicles;

public class Bus : Vehicle
{
    public int CompletedTrips { get; set; }
    public LaneSegment StationA { get; set; }
    public LaneSegment StationB { get; set; }

    public Bus()
    {
        CompletedTrips = 0;
    }

    public void BusOnSegment(LaneSegment segment)
    {
        if (segment.SnowLevel >= 3)
        {
            this.isBlocked = true;
        }
    }
}