using SnowPlow.Model.Map;
using SnowPlow.Model.Vehicles;
using UnityEngine;

namespace SnowPlow.Controller.NPCMovement
{
    public class VehicleSegmentSensor : MonoBehaviour
    {
        private Vehicle vehicle;
        private VisualSegment currentVisualSegment;
        private LanePosition currentLanePosition;

        public void Initialize(Vehicle vehicleModel)
        {
            vehicle = vehicleModel;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            VisualSegment visualSegment = other.GetComponent<VisualSegment>();

            if (visualSegment == null)
            {
                return;
            }

            LanePosition lanePosition = visualSegment.LanePosition;

            if (lanePosition == null)
            {
                return;
            }

            if (currentLanePosition != null && currentLanePosition.Equals(lanePosition))
            {
                return;
            }

            if (currentLanePosition != null && currentVisualSegment != null)
            {
                SegmentHandler.OnVehicleExitSegment(vehicle, currentLanePosition, currentVisualSegment);
            }

            currentVisualSegment = visualSegment;
            currentLanePosition = lanePosition;

            SegmentHandler.OnVehicleEnterSegment(vehicle, currentLanePosition, currentVisualSegment);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            VisualSegment visualSegment = other.GetComponent<VisualSegment>();

            if (visualSegment == null)
            {
                return;
            }

            if (currentVisualSegment != visualSegment)
            {
                return;
            }

            SegmentHandler.OnVehicleExitSegment(vehicle, currentLanePosition, currentVisualSegment);

            currentVisualSegment = null;
            currentLanePosition = null;
        }
    }
}