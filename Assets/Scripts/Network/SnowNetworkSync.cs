using Unity.Netcode;
using UnityEngine;
using SnowPlow.Model.Map;
using SnowPlow.Model.Map.Generator;

public class SnowNetworkSync : NetworkBehaviour
{
    [SerializeField]
    private MapVisualizer mapVisualizer;

    public override void OnNetworkSpawn()
    {
        if (mapVisualizer == null)
        {
            mapVisualizer = FindObjectOfType<MapVisualizer>();
        }
    }

    [ClientRpc]
    public void UpdateSnowClientRpc(
        int roadId,
        int laneId,
        int segmentIndex,
        int snowLevel,
        bool hasIce)
    {
        LaneSegment segment =
            mapVisualizer.GetSegment(
                roadId,
                laneId,
                segmentIndex);

        segment.SnowLevel = snowLevel;
        segment.SetIce(hasIce);

        mapVisualizer.GetVisual(
            roadId,
            laneId,
            segmentIndex)
            .UpdateVisuals();
    }
}