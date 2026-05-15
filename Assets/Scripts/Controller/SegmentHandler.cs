using SnowPlow.Model.Map;
using SnowPlow.Model.Vehicles;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using SnowPlowVehicle = SnowPlow.Model.Vehicles.SnowPlow;

public static class SegmentHandler
{
    public static void OnVehicleEnterSegment(
        Vehicle vehicle,
        LanePosition position,
        VisualSegment visual = null)
    {
        if (vehicle == null || position == null) return;

        LaneSegment segment = position.Lane[position.SegmentIndex];

        vehicle.CurrentPosition = position;

        segment.VehicleCount++;

        if (vehicle is Bus bus)
        {
            bus.BusOnSegment(segment);
        }

        if (vehicle is SnowPlowVehicle snowPlow)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                snowPlow.ApplyToolEffect(position);
            }
        }

        visual?.UpdateVisuals();
    }

    public static void OnVehicleExitSegment(
        Vehicle vehicle,
        LanePosition position,
        VisualSegment visual = null)
    {
        if (vehicle == null || position == null) return;

        LaneSegment segment = position.Lane[position.SegmentIndex];

        switch (vehicle)
        {
            case Bus bus:
                bus.isBlocked = false;
                break;
        }

        visual?.UpdateVisuals();
    }

    public static void OnVehicleExitSegment(
        Vehicle vehicle,
        LaneSegment segment,
        VisualSegment visual = null)
    {
        if (vehicle == null || segment == null) return;

        switch (vehicle)
        {
            case Bus bus:
                bus.isBlocked = false;
                break;
        }

        visual?.UpdateVisuals();
    }
}