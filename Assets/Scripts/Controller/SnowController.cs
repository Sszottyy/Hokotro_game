using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Controller;
using SnowPlow.Model.Map;
using UnityEngine;

namespace Assets.Scripts.Controller
{
    public class SnowController : MonoBehaviour
    {
        private SnowSystem SnowSystem { get; set; }

        public void Init(List<Lane> lanes)
        {
            SnowSystem = new SnowSystem(lanes);
        }


        void Update()
        {
            if (SnowSystem != null) SnowSystem.Update(Time.deltaTime);
        }

    }
}
