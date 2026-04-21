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
        private SnowSystem snowSystem;

        public void Init(List<Lane> lanes)
        {
            snowSystem = new SnowSystem(lanes);
        }


        void Update()
        {
            if (snowSystem != null) snowSystem.Update(Time.deltaTime);
        }

    }
}
