using SnowPlow.Controller.Shop;
using SnowPlow.Model.Players;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class ShopDebugTester : MonoBehaviour
{
    [SerializeField] private ShopController shopController;
    [SerializeField] private int testMoney = 5000;
    [SerializeField] private bool makeShopVisible = true;

    private void Awake()
    {
        EnsureTestPlayerAndTeam();
    }

    private IEnumerator Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("ShopDebugTester: GameManager is missing.");
            yield break;
        }

        if (GameManager.Instance.CurrentPlayer == null)
        {
            Debug.LogWarning("ShopDebugTester: CurrentPlayer is missing after Awake setup.");
            yield break;
        }

        if (GameManager.Instance.CurrentPlayer.Team == null)
        {
            Debug.LogWarning("ShopDebugTester: CurrentPlayer has no Team after Awake setup.");
            yield break;
        }

        if (shopController == null)
        {
            Debug.LogWarning("ShopDebugTester: ShopController is missing.");
            yield break;
        }

        shopController.SetVisibleForSnowPlowPlayer(makeShopVisible);
        shopController.RefreshUI();

        Player player = GameManager.Instance.CurrentPlayer;

        Debug.Log("ShopDebugTester ready. Current money: " + player.Team.Money);

        yield return new WaitUntil(() =>
            GameManager.Instance != null &&
            GameManager.Instance.CurrentPlayer != null &&
            GameManager.Instance.CurrentPlayer.GetOwnedSnowPlow() != null
        );

        Debug.Log("ShopDebugTester: player SnowPlow is ready.");
        Debug.Log("Player vehicle count: " + GameManager.Instance.CurrentPlayer.Vehicles.Count);

        shopController.RefreshUI();
    }

    private void EnsureTestPlayerAndTeam()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("ShopDebugTester: GameManager is missing in Awake.");
            return;
        }

        if (GameManager.Instance.CurrentPlayer == null)
        {
            GameManager.Instance.CreatePlayer("ShopTestPlayer","Team A",PlayerRole.SnowPlowDriver, 1);
        }

        Player player = GameManager.Instance.CurrentPlayer;

        if (player == null)
        {
            Debug.LogWarning("ShopDebugTester: failed to create test player.");
            return;
        }

        if (player.Team == null)
        {
            Team testTeam = new Team
            {
                Name = "ShopTestTeam"
            };

            testTeam.AddMoney(testMoney);
            player.Team = testTeam;
        }
        else
        {
            player.Team.AddMoney(testMoney);
        }
    }

    [ContextMenu("Add Test Money")]
    public void AddTestMoney()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentPlayer == null) return;
        if (GameManager.Instance.CurrentPlayer.Team == null) return;

        GameManager.Instance.CurrentPlayer.Team.AddMoney(testMoney);

        if (shopController != null)
        {
            shopController.RefreshUI();
        }

        Debug.Log("Added test money: " + testMoney);
    }

    [ContextMenu("Refresh Shop UI")]
    public void RefreshShopUI()
    {
        if (shopController != null)
        {
            shopController.RefreshUI();
        }
    }
}