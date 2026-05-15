using SnowPlow.Model.Map;
using SnowPlow.Model.Map.Generator;
using SnowPlow.Model.Players;
using SnowPlow.Model.Tools;
using Unity.Netcode;
using UnityEngine;

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
    bool hasIce,
    int saltPower)
    {
        //Debug.Log("CLIENT RPC RECEIVED");
       // Debug.Log("mapVisualizer null? " + (mapVisualizer == null));
        LaneSegment segment =
            mapVisualizer.GetSegment(
                roadId,
                laneId,
                segmentIndex);

        segment.SnowLevel = snowLevel;
        segment.SetIce(hasIce);
        segment.SetSaltPower(saltPower);

        mapVisualizer.GetVisual(
            roadId,
            laneId,
            segmentIndex)
            .UpdateVisuals();
    }

    
    [ServerRpc(RequireOwnership = false)]
    public void SyncSegmentServerRpc(
    int roadId,
    int laneId,
    int segmentIndex,
    int snowLevel,
    bool hasIce,
    int saltPower)
    {
        UpdateSnowClientRpc(
            roadId,
            laneId,
            segmentIndex,
            snowLevel,
            hasIce,
            saltPower
        );
    }

    [ServerRpc(RequireOwnership = false)]
    public void EquipToolServerRpc(
    ulong ownerClientId,
    int toolType)
    {
        Player player =
            GameManager.Instance.GetPlayer(ownerClientId);

        if (player == null)
            return;

        var snowPlow = player.GetOwnedSnowPlow();

        if (snowPlow == null)
            return;

        var tool =
            player.FindOwnedTool((PlowToolType)toolType);

        if (tool == null)
            return;

        snowPlow.EquippedTool = tool;

        Debug.Log("SERVER equipped: " + tool.Type());
    }
}