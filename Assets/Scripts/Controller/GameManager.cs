using SnowPlow.Model.Players;
using System.Collections.Generic;
using SnowPlowVehicle = SnowPlow.Model.Vehicles.SnowPlow;
using SnowPlow.Model.Vehicles;
using UnityEngine;
using SnowPlow.Model.Map.Generator;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool GameEnded = false;
    public MapData CurrentMap { get; set; }
    public Player CurrentPlayer { get; private set; }

    public List<Player> Players { get; private set; } = new List<Player>();
    //ket csapat
    public Team TeamA { get; private set; } = new Team() { Name = "Team A" };
    public Team TeamB { get; private set; } = new Team() { Name = "Team B" };

    public Player LocalPlayer
    {
        get
        {
            if (Unity.Netcode.NetworkManager.Singleton == null)
                return null;

            ulong localClientId =
                Unity.Netcode.NetworkManager.Singleton.LocalClientId;

            return Players.Find(
                p => p.OwnerClientId == localClientId);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CreatePlayer(string name, string teamName, PlayerRole role, ulong clientId)
    {
        Team selectedTeam = (teamName == "Team A") ? TeamA : TeamB;
        Player newPlayer = new Player(name, selectedTeam);
        newPlayer.Role = role;
        newPlayer.OwnerClientId = clientId;
        if (role == PlayerRole.SnowPlowDriver)
        {
            SnowPlowVehicle plow = new SnowPlowVehicle();
            newPlayer.AddVehicle(plow);
        }
        else if (role == PlayerRole.BusDriver)
        {
            Bus bus = new Bus();
            newPlayer.AddVehicle(bus);
        }
        Players.Add(newPlayer);
       // CurrentPlayer = newPlayer;
        Debug.Log($"Player created: {newPlayer.Name}");
    }

    public void RemoveCurrentPlayer()
    {
        Player localPlayer = LocalPlayer;

        if (localPlayer == null)
            return;

        localPlayer.Team = null;

        Players.Remove(localPlayer);

        Debug.Log(
            "Removed player: " +
            localPlayer.Name +
            " from Lobby");
    }
    public Player GetPlayer(ulong clientId)
    {
        return Players.Find(p => p.OwnerClientId == clientId);
    }
    public void RemovePlayerByClientId(ulong clientId)
    {
        Player playerToRemove = Players.Find(p => p.OwnerClientId == clientId);

        if (playerToRemove != null)
        {
            Players.Remove(playerToRemove);

            Debug.Log($"Removed player: {playerToRemove.Name}");
        }
    }
}