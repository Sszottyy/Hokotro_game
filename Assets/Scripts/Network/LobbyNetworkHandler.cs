using SnowPlow.Controller.Spawning;
using SnowPlow.Model.Map.Generator;
using SnowPlow.Model.Players;
using SnowPlow.Model.Tools;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyNetworkHandler : NetworkBehaviour
{
    public static LobbyNetworkHandler Instance { get; private set; }
    public NetworkVariable<int> MapSeed =
    new NetworkVariable<int>();
    public NetworkVariable<int> IntersectionCount =
    new NetworkVariable<int>(10);
    void Awake()
    {
        Debug.Log("LobbyNetworkHandler Awake called");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("LobbyNetworkHandler Instance set");
        }
        else
        {
            Debug.LogWarning("Duplicate LobbyNetworkHandler detected, destroying this one");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log($"LobbyNetworkHandler Start - IsSpawned: {IsSpawned}");

        // Host/server esetén spawnoljuk be
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsServer &&
            !IsSpawned)
        {
            Debug.Log("Spawning LobbyNetworkHandler...");

            GetComponent<NetworkObject>().Spawn();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"LobbyNetworkHandler OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, IsHost: {IsHost}");

        // Ha valaki disconnectel
        if (IsServer)
        {
            MapSeed.Value =
                UnityEngine.Random.Range(
                    int.MinValue,
                    int.MaxValue);

            Debug.Log(
                $"[MAP] GENERATED SEED: {MapSeed.Value}"
            );

            NetworkManager.OnClientDisconnectCallback +=
                OnClientDisconnected;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetIntersectionCountServerRpc(int count)
    {
        count = Mathf.Clamp(count, 5, 100);

        IntersectionCount.Value = count;

        Debug.Log(
            $"[MAP] INTERSECTION COUNT SET TO: {count}"
        );
    }
    public void GenerateMapForAll()
    {
        Debug.Log(
            $"[MAP] GENERATING WITH SEED: {MapSeed.Value}"
        );

        MapGenerator generator =
            new MapGenerator(MapSeed.Value);

        MapData map =
    generator.Generate(IntersectionCount.Value);

        GameManager.Instance.CurrentMap = map;

        MapVisualizer visualizer =
            FindObjectOfType<MapVisualizer>();

        if (visualizer != null)
        {
            // régi map törlése
            for (int i = visualizer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(
                    visualizer.transform.GetChild(i).gameObject
                );
            }

            visualizer.Visualize(map);

            VehicleSpawner spawner =
                FindObjectOfType<VehicleSpawner>();

            if (spawner != null)
            {
                spawner.Initialize(map, visualizer);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // =========================
    // PLAYER CREATE
    // =========================

    [ServerRpc(RequireOwnership = false)]
    public void CreatePlayerServerRpc(
        string playerName,
        string selectedTeam,
        PlayerRole selectedRole,
        ulong clientId)
    {
        Debug.Log($"CreatePlayerServerRpc called. Player: {playerName}");

        // Duplikáció védelem
        foreach (Player p in GameManager.Instance.Players)
        {
            if (p.OwnerClientId == clientId)
            {
                Debug.LogWarning($"Client {clientId} already has a player!");
                return;
            }
        }

        GameManager.Instance.CreatePlayer(
            playerName,
            selectedTeam,
            selectedRole,
            clientId);

        SendLobbyUpdate();
    }

    // =========================
    // PLAYER REMOVE
    // =========================

    [ServerRpc(RequireOwnership = false)]
    public void RemovePlayerServerRpc(ulong clientId)
    {
        Debug.Log($"RemovePlayerServerRpc: {clientId}");

        RemovePlayer(clientId);

        SendLobbyUpdate();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");

        RemovePlayer(clientId);

        SendLobbyUpdate();
    }

    private void RemovePlayer(ulong clientId)
    {
        List<Player> players = GameManager.Instance.Players;

        Player playerToRemove = null;

        foreach (Player p in players)
        {
            if (p.OwnerClientId == clientId)
            {
                playerToRemove = p;
                break;
            }
        }

        if (playerToRemove != null)
        {
            Debug.Log($"Removing player: {playerToRemove.Name}");

            players.Remove(playerToRemove);
        }
    }

    // =========================
    // LOBBY UPDATE
    // =========================

    private void SendLobbyUpdate()
    {
        string[] names = new string[4];
        string[] teams = new string[4];
        PlayerRole[] roles = new PlayerRole[4];
        ulong[] clientIds = new ulong[4];

        int count = Mathf.Min(GameManager.Instance.Players.Count, 4);

        for (int i = 0; i < count; i++)
        {
            Player p = GameManager.Instance.Players[i];

            names[i] = p.Name;
            teams[i] = p.Team.Name;
            roles[i] = p.Role;
            clientIds[i] = p.OwnerClientId;
        }

        UpdateLobbyUIClientRpc(
            names[0],
            names[1],
            names[2],
            names[3],

            teams[0],
            teams[1],
            teams[2],
            teams[3],

            roles[0],
            roles[1],
            roles[2],
            roles[3],

            clientIds[0],
            clientIds[1],
            clientIds[2],
            clientIds[3],

            count
        );
    }

    [ClientRpc]
    public void UpdateLobbyUIClientRpc(
    string name1,
    string name2,
    string name3,
    string name4,

    string team1,
    string team2,
    string team3,
    string team4,

    PlayerRole role1,
    PlayerRole role2,
    PlayerRole role3,
    PlayerRole role4,

    ulong id1,
    ulong id2,
    ulong id3,
    ulong id4,

    int len)
    {
        Debug.Log($"UpdateLobbyUIClientRpc called. Player count: {len}");

        //GameManager.Instance.Players.Clear();

        if (len > 0 && !string.IsNullOrEmpty(name1))
        {
            if (GameManager.Instance.GetPlayer(id1) == null)
            {
                GameManager.Instance.CreatePlayer(
                    name1,
                    team1,
                    role1,
                    id1
                );
            }
        }

        if (len > 1 && !string.IsNullOrEmpty(name2))
        {
            if (GameManager.Instance.GetPlayer(id2) == null)
            {
                GameManager.Instance.CreatePlayer(
                name2,
                team2,
                role2,
                id2
            );
            }
        }

        if (len > 2 && !string.IsNullOrEmpty(name3))
        {
            if (GameManager.Instance.GetPlayer(id3) == null)
            {
                GameManager.Instance.CreatePlayer(
                name3,
                team3,
                role3,
                id3
            );
            }
        }

        if (len > 3 && !string.IsNullOrEmpty(name4))
        {
            if (GameManager.Instance.GetPlayer(id4) == null)
            {
                GameManager.Instance.CreatePlayer(
                name4,
                team4,
                role4,
                id4
            );
            }
        }

        MainMenu mainMenu = FindObjectOfType<MainMenu>(true);

        if (mainMenu != null)
        {
            mainMenu.UpdateLobbyUIFromData(
                name1,
                name2,
                name3,
                name4,

                team1,
                team2,
                team3,
                team4,

                role1,
                role2,
                role3,
                role4,

                len);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void EquipToolServerRpc(
    ulong clientId,
    int toolType)
    {
        EquipToolClientRpc(clientId, toolType);
    }

    [ClientRpc (RequireOwnership =false)]
    private void EquipToolClientRpc(ulong clientId, int toolType)
    {
        Player player =
           GameManager.Instance.Players.Find(
               p => p.OwnerClientId == clientId);

        if (player == null)
        {
            Debug.LogWarning("Player not found.");
            return;
        }

        var snowPlow = player.GetOwnedSnowPlow();

        if (snowPlow == null)
        {
            Debug.LogWarning("Snowplow not found.");
            return;
        }

        var tool =
           player.FindOwnedTool(
               (PlowToolType)toolType);

        if (tool == null)
        {
            Debug.LogWarning("Tool not owned.");
            return;
        }

        snowPlow.EquippedToolType =
                (PlowToolType)toolType;

        snowPlow.EquippedTool = tool;

        PlowMovement[] allPlowsOnMap = FindObjectsByType<PlowMovement>(FindObjectsSortMode.None);

        foreach (PlowMovement movementScript in allPlowsOnMap)
        {
            if (movementScript.GetPlowModel() == snowPlow)
            {
                movementScript.SetEquippedToolType((PlowToolType)toolType);
                Debug.Log("Updated visual on the exact player's screen for: " + toolType);
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuyToolServerRpc(
    ulong ownerClientId,
    int toolType)
    {
        Player player =
            GameManager.Instance.GetPlayer(ownerClientId);

        if (player == null)
            return;

        IPlowTool tool = null;

        switch ((PlowToolType)toolType)
        {
            case PlowToolType.Sweaper:
                tool = new SweaperTool();
                break;

            case PlowToolType.IceBreaker:
                tool = new IceBreaker();
                break;

            case PlowToolType.Vomit:
                tool = new VomitTool();
                break;

            case PlowToolType.Salt:
                tool = new SaltTool();
                break;

            case PlowToolType.Dragon:
                tool = new DragonTool();
                break;
        }

        if (tool == null)
            return;

        player.AddPlowTool(tool);

        Debug.Log(
            "[SERVER] Tool bought: " +
            tool.Type());
        BuyToolClientRpc(ownerClientId, toolType);
    }
    [ClientRpc]
    private void BuyToolClientRpc(
ulong ownerClientId,
int toolType)
    {
        Player player =
            GameManager.Instance.GetPlayer(ownerClientId);

        if (player == null)
            return;

        IPlowTool tool = ((PlowToolType)toolType) switch
        {
            PlowToolType.Sweaper => new SweaperTool(),

            PlowToolType.IceBreaker => new IceBreaker(),

            PlowToolType.Vomit => new VomitTool(),

            PlowToolType.Salt => new SaltTool(),

            PlowToolType.Dragon => new DragonTool(),

            _ => null
        };

        if (tool == null)
            return;

        player.AddPlowTool(tool);

        Debug.Log(
            "[CLIENT] Tool synced: " +
            tool.Type());
    }
    [ServerRpc(RequireOwnership = false)]
    public void BuyNpcSnowPlowServerRpc(int toolType)
    {
        VehicleSpawner vehicleSpawner =
            FindObjectOfType<VehicleSpawner>();

        if (vehicleSpawner == null)
        {
            Debug.LogError(
                "VehicleSpawner not found on server!"
            );

            return;
        }

        IPlowTool tool = ((PlowToolType)toolType) switch
        {
            PlowToolType.Sweaper => new SweaperTool(),

            PlowToolType.IceBreaker => new IceBreaker(),

            _ => null
        };

        if (tool == null)
        {
            Debug.LogError(
                "Invalid NPC tool type!"
            );

            return;
        }

        Debug.Log(
            "[SERVER] Spawning NPC snowplow..."
        );

        vehicleSpawner.SpawnSnowPlowNPC(tool);
    }



}