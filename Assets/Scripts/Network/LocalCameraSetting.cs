using Unity.Netcode;
using UnityEngine;

public class LocalPlayerCameraTarget : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        CameraFollow cam = Camera.main.GetComponent<CameraFollow>();

        if (cam != null)
        {
            cam.SetTarget(transform);

            Debug.Log("[Camera] Locked to local owned object");
        }
    }
}