using SnowPlow.Controller;
using SnowPlow.Model.Map;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Controller
{
    public class SnowController : MonoBehaviour
    {
        [SerializeField]
        private SnowNetworkSync snowSync;
        private SnowSystem snowSystem { get; set; }

        public void Init(List<Lane> lanes)
        {
            snowSystem = new SnowSystem(lanes, snowSync);

            if (NetworkManager.Singleton.IsServer)
            {
                //snowSystem.GenerateInitialSnow();
            }
        }


        void Update()
        {
            //csak a host kezelje a havat (tobbiek csak megjelenítenek)
            if (!NetworkManager.Singleton.IsServer)
                return;
            if (!Unity.Netcode.NetworkManager.Singleton.IsServer)
                return;

            if (snowSystem == null) return;
            snowSystem?.Update(Time.deltaTime);
        }

    }
}
