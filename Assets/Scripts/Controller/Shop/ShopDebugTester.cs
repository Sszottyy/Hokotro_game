using SnowPlow.Controller.Shop;
using SnowPlow.Model.Players;
using System.Collections;
using UnityEngine;

public class ShopDebugTester : MonoBehaviour
{
    [SerializeField] private ShopController shopController;
    [SerializeField] private int testMoney = 5000;
    [SerializeField] private bool makeShopVisible = true;

    private IEnumerator Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("ShopDebugTester: GameManager is missing.");
            yield break;
        }

        if (GameManager.Instance.CurrentPlayer == null)
        {
            GameManager.Instance.CreatePlayer("ShopTestPlayer");
        }

        Player player = GameManager.Instance.CurrentPlayer;

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

        if (shopController == null)
        {
            Debug.LogWarning("ShopDebugTester: ShopController is missing.");
            yield break;
        }

        shopController.SetVisibleForSnowPlowPlayer(makeShopVisible);
        shopController.RefreshUI();

        Debug.Log("ShopDebugTester ready. Current money: " + player.Team.Money);

        yield return null;
        shopController.RefreshUI();

        yield return new WaitUntil(() =>
            GameManager.Instance != null &&
            GameManager.Instance.CurrentPlayer != null &&
            GameManager.Instance.CurrentPlayer.GetOwnedSnowPlow() != null
        );

        Debug.Log("ShopDebugTester: player SnowPlow is ready.");

        shopController.RefreshUI();
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