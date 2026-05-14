using System.Collections.Generic;
using SnowPlow.Model.Players;
using Unity.Netcode;
using UnityEngine;

public class LobbyNetworkHandler : NetworkBehaviour
{
    public static LobbyNetworkHandler Instance { get; private set; }

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
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
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

        int count = Mathf.Min(GameManager.Instance.Players.Count, 4);

        for (int i = 0; i < count; i++)
        {
            Player p = GameManager.Instance.Players[i];

            names[i] = p.Name;
            teams[i] = p.Team.Name;
            roles[i] = p.Role;
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

        int len)
    {
        Debug.Log($"UpdateLobbyUIClientRpc called. Player count: {len}");

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
        else
        {
            Debug.LogError("MainMenu not found in scene!");
        }
    }
}