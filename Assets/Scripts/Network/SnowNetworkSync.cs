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
        if (mapVisualizer == null)
        {
            return;
        }

        if (!mapVisualizer.HasSegment(
                roadId,
                laneId,
                segmentIndex))
        {
            Debug.LogWarning(
                $"Segment not ready yet: ({roadId},{laneId},{segmentIndex})");

            return;
        }

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
        snowPlow.EquippedToolType = (PlowToolType)toolType;

        Debug.Log("SERVER equipped: " + tool.Type());
        UpdateToolVisualClientRpc(
    ownerClientId,
    toolType);
    }
    [ClientRpc]
    public void UpdateToolVisualClientRpc(
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

        PlowToolType type =
            (PlowToolType)toolType;
        PlowMovement[] allPlows =
            FindObjectsByType<PlowMovement>(
                FindObjectsSortMode.None);

        foreach (var plow in allPlows)
        {
            if (plow.OwnerClientId.Value == ownerClientId)
            {
                Debug.Log($"MATCHED PLOW FOR CLIENT {ownerClientId}");

                // MODELL LEKÉRÉS
                var model = plow.GetPlowModel();

                if (model != null)
                {
                    model.EquippedToolType = type;

                    switch (type)
                    {
                        case PlowToolType.Sweaper:
                            model.EquippedTool = new SweaperTool();
                            break;

                        case PlowToolType.IceBreaker:
                            model.EquippedTool = new IceBreaker();
                            break;

                        case PlowToolType.Vomit:
                            model.EquippedTool = new VomitTool();
                            break;

                        case PlowToolType.Salt:
                            model.EquippedTool = new SaltTool();
                            break;

                        case PlowToolType.Dragon:
                            model.EquippedTool = new DragonTool();
                            break;
                    }
                }

                // EZ A LÉNYEG
                plow.LateInitialize();

                // VIZUÁL FRISSÍTÉS
                plow.SetEquippedToolType(type);
                plow.UpdateEquippedToolVisual();

                Debug.Log("CLIENT visual updated: " + type);

                break;
            }
        }


        foreach (var p in allPlows)
        {
            Debug.Log(
                $"PLOW FOUND -> Owner: {p.OwnerClientId.Value}");
        }
        Debug.Log($"SEARCH OWNER: {ownerClientId}");

       
    }
}