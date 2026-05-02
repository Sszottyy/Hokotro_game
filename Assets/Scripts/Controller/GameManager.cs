using SnowPlow.Model.Players;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Player CurrentPlayer { get; private set; }

    public List<Player> Players { get; private set; } = new List<Player>();

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

    public void CreatePlayer(string name)
    {
        Player newPlayer = new Player(name, team: null);
        Players.Add(newPlayer);
        CurrentPlayer = newPlayer;
        Debug.Log($"Player created: {newPlayer.Name}");
    }

    public void RemoveCurrentPlayer()
    {
        Players.Remove(CurrentPlayer);
        Debug.Log("Removed player:" + CurrentPlayer.Name + " from Lobby");
    }
}