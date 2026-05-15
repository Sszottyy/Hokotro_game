using SnowPlow.Controller.Shop;
using SnowPlow.Controller.Spawning;
using SnowPlow.Model.Players;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class GameSessionController : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private GameTimerController gameTimer;

    [Header("References")]
    [SerializeField] private ShopController shopController;
    [SerializeField] private VehicleSpawner vehicleSpawner;

    [Header("Local Game Session")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private PlayerRole playerRole = PlayerRole.SnowPlowDriver;
    [SerializeField] private int startingMoney = 0;


    public TMPro.TextMeshProUGUI timerText;
    public TMPro.TextMeshProUGUI scoreText;

    private void Awake()
    {
        EnsurePlayer();
        ConfigureVehicleSpawner();
    }

    private void Start()
    {
        gameTimer.OnTimerEnded += EndGame;
        ApplyShopVisibility();
    }

    private void Update()
    {
        if (gameTimer == null || timerText == null) return;
        timerText.text = gameTimer.GetFormattedTime();

        if (scoreText != null)
        {
            int score = GameManager.Instance?.CurrentPlayer?.Team?.Score ?? 0;
            scoreText.text = $"{score}";
        }
    }

    private void EndGame()
    {
        Debug.Log("Game ended!");

        Time.timeScale = 1f; // reset before scene load
        GameManager.Instance.GameEnded = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }

    private void EnsurePlayer()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameSessionController: GameManager is missing from the scene.");
            return;
        }

        if (GameManager.Instance.CurrentPlayer == null)
        {
            GameManager.Instance.CreatePlayer(playerName,"Team A",PlayerRole.SnowPlowDriver);
        }

        Player player = GameManager.Instance.CurrentPlayer;

        if (player == null)
        {
            Debug.LogError("GameSessionController: CurrentPlayer could not be created.");
            return;
        }

        player.Role = playerRole;

        if (player.Team == null)
        {
            Team team = new Team
            {
                Name = "LocalTeam"
            };

            if (startingMoney > 0)
            {
                team.AddMoney(startingMoney);
            }

            player.Team = team;
        }
        else if (startingMoney > 0)
        {
            player.Team.AddMoney(startingMoney);
        }

        Debug.Log($"GameSessionController: Player ready. Name={player.Name}, Role={player.Role}, Money={player.Team?.Money}");
    }

    private void ConfigureVehicleSpawner()
    {
        if (vehicleSpawner == null)
        {
            Debug.LogWarning("GameSessionController: VehicleSpawner is missing.");
            return;
        }

        bool spawnSnowPlow = playerRole == PlayerRole.SnowPlowDriver;
        bool spawnBus = playerRole == PlayerRole.BusDriver;

        vehicleSpawner.ConfigurePlayerSpawn(spawnSnowPlow, spawnBus);
    }

    private void ApplyShopVisibility()
    {
        if (shopController == null)
        {
            Debug.LogWarning("GameSessionController: ShopController is missing.");
            return;
        }

        bool isSnowPlowPlayer = playerRole == PlayerRole.SnowPlowDriver;

        shopController.SetVisibleForSnowPlowPlayer(isSnowPlowPlayer);
        shopController.RefreshUI();
    }
}