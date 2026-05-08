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

        // Ha szerver vagy és nincs spawnolva, spawnold be
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !IsSpawned)
        {
            Debug.Log("Spawning LobbyNetworkHandler...");
            GetComponent<NetworkObject>().Spawn();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"LobbyNetworkHandler OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, IsHost: {IsHost}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreatePlayerServerRpc(string playerName, string selectedTeam, PlayerRole selectedRole, ulong clientId)
    {
        Debug.Log($"CreatePlayerServerRpc called on server. Player: {playerName}, Team: {selectedTeam}, Role: {selectedRole}");

        GameManager.Instance.CreatePlayer(playerName, selectedTeam, selectedRole);

        // Adatok elõkészítése
        string[] name = new string[4];
        string[] team = new string[4];
        PlayerRole[] role = new PlayerRole[4];

        int playerCount = GameManager.Instance.Players.Count;
        for (int i = 0; i < playerCount && i < 4; i++)
        {
            name[i] = GameManager.Instance.Players[i].Name;
            team[i] = GameManager.Instance.Players[i].Team.Name;
            role[i] = GameManager.Instance.Players[i].Role;
        }

        // Minden kliensnek elküldjük
        UpdateLobbyUIClientRpc(
            name[0], name[1], name[2], name[3],
            team[0], team[1], team[2], team[3],
            role[0], role[1], role[2], role[3],
            playerCount);
    }

    [ClientRpc]
    public void UpdateLobbyUIClientRpc(string name1, string name2, string name3, string name4,
        string team1, string team2, string team3, string team4,
        PlayerRole role1, PlayerRole role2, PlayerRole role3, PlayerRole role4,
        int len)
    {
        Debug.Log($"UpdateLobbyUIClientRpc called. Player count: {len}");

        MainMenu mainMenu = FindObjectOfType<MainMenu>(true);
        if (mainMenu != null)
        {
            mainMenu.UpdateLobbyUIFromData(name1, name2, name3, name4,
                team1, team2, team3, team4,
                role1, role2, role3, role4, len);
        }
        else
        {
            Debug.LogError("MainMenu not found in scene!");
        }
    }
}