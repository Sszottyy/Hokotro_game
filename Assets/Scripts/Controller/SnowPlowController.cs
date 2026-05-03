using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SnowPlow.Model.Vehicles;
using SnowPlowVehicle = SnowPlow.Model.Vehicles.SnowPlow;


namespace SnowPlow.Controller
{


    public class SnowPlowController : MonoBehaviour
    {
        public SnowPlowVehicle Model { get; private set; }

        private void Awake()
        {
            Model = new SnowPlowVehicle();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var segmentView = other.GetComponent<VisualSegment>();

            if (segmentView != null && segmentView.LogicSegment != null)
            {
                Model.ApplyToolEffect(segmentView.LogicSegment);

                // ideiglenesen:
                segmentView.UpdateVisuals();
            }
        }
    }
}
