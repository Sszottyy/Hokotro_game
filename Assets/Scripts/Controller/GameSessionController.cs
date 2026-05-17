using SnowPlow.Controller.Shop;
using SnowPlow.Controller.Spawning;
using SnowPlow.Model.Players;
using System.Collections;
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
    [SerializeField] private int startingMoney = 5000;


    public TMPro.TextMeshProUGUI timerText;
    public TMPro.TextMeshProUGUI scoreText;
    public TMPro.TextMeshProUGUI moneyText;

    private IEnumerator Start()
    {
        while (GameManager.Instance == null ||
               GameManager.Instance.LocalPlayer == null)
        {
            yield return null;
        }

        EnsurePlayer();
        //ConfigureVehicleSpawner();

        gameTimer.OnTimerEnded += EndGame;

        // FONTOS: várunk hogy a role/network biztosan betöltõdjön
        yield return new WaitForSeconds(1f);
        if (LobbyNetworkHandler.Instance != null)
        {
            LobbyNetworkHandler.Instance.GenerateMapForAll();
        }
        Debug.Log(
            "[SESSION ROLE CHECK] role = " +
            GameManager.Instance.LocalPlayer.Role
        );

        //ApplyShopVisibility();
    }

    /*private void Start()
    {
        gameTimer.OnTimerEnded += EndGame;
        ApplyShopVisibility();
    }*/

    private void Update()
    {
        if (gameTimer != null && timerText != null)
        {
            timerText.text = gameTimer.GetFormattedTime();
        }

        if (scoreText != null)
        {
            int score =
                GameManager.Instance?.LocalPlayer?.Team?.Score ?? 0;

            scoreText.text = $"{score}";
        }

        if (moneyText != null)
        {
            int money =
                GameManager.Instance?.LocalPlayer?.Team?.Money ?? 0;

            moneyText.text = $"{money}";
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
            return;

        Player player = GameManager.Instance.LocalPlayer;

        if (player == null)
            return;

        if (player.Team != null && player.Team.Money <= 0)
        {
            player.Team.AddMoney(startingMoney);

            Debug.Log(
                $"[SESSION] START MONEY ADDED = {startingMoney}"
            );
        }

        Debug.Log(
            $"[SESSION] Player={player.Name} Team={player.Team?.Name} Money={player.Team?.Money}"
        );

        if (shopController != null)
        {
            shopController.RefreshUI();
        }
    }

    /*private void ConfigureVehicleSpawner()
    {
        if (vehicleSpawner == null)
        {
            Debug.LogWarning("GameSessionController: VehicleSpawner is missing.");
            return;
        }

        //bool spawnSnowPlow = playerRole == PlayerRole.SnowPlowDriver;
        //bool spawnBus = playerRole == PlayerRole.BusDriver;
        PlayerRole actualRole =
    GameManager.Instance.LocalPlayer.Role;

        bool spawnSnowPlow =
            actualRole == PlayerRole.SnowPlowDriver;

        bool spawnBus =
            actualRole == PlayerRole.BusDriver;

        vehicleSpawner.ConfigurePlayerSpawn(spawnSnowPlow, spawnBus);
    }*/

    /*private void ApplyShopVisibility()
    {
        if (shopController == null)
        {
            Debug.LogWarning("GameSessionController: ShopController is missing.");
            return;
        }

        //bool isSnowPlowPlayer = playerRole == PlayerRole.SnowPlowDriver;
        bool isSnowPlowPlayer =
        GameManager.Instance.LocalPlayer.Role
        == PlayerRole.SnowPlowDriver;

        shopController.SetVisibleForSnowPlowPlayer(isSnowPlowPlayer);
        shopController.RefreshUI();
    }*/
}