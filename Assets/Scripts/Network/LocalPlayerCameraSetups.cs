using Unity.Netcode;
using UnityEngine;

public class LocalPlayerCameraSetup : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        CameraFollow cameraFollow =
            Camera.main?.GetComponent<CameraFollow>();

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);

            Debug.Log("Camera locked to local player.");
        }
        else
        {
            Debug.LogError("CameraFollow not found.");
        }
    }
}