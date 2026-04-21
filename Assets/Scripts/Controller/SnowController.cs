using System;
using System.Collections.Generic;
using System.Text;
using SnowPlow.Controller;
using UnityEngine;

namespace Assets.Scripts.Controller
{
    public class SnowController : MonoBehaviour
    {
        private SnowSystem snowSystem;

        void Start()
        {
        }

        void Update()
        {
            if (snowSystem != null) snowSystem.Update(Time.deltaTime);
        }

        public void SetSnowSystem(SnowSystem system)
        {
            snowSystem = system;
        }
    }
}
