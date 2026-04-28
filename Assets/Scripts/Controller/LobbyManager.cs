using SnowPlow.Model.Players;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public Transform playerListA;     // Team A (vagy csapat nélküli)
    public Transform playerListB;     // Team B
    public GameObject playerRowPrefab;

    void Start()
    {
        RefreshLobby();
    }
    
    void OnEnable()
    {
        RefreshLobby();
    }

    public void RefreshLobby()
    {
        // Töröljük mindkét lista tartalmát
        foreach (Transform child in playerListA)
            Destroy(child.gameObject);
        foreach (Transform child in playerListB)
            Destroy(child.gameObject);

        Debug.Log("Players count: " + GameManager.Instance.Players.Count);

        foreach (Player player in GameManager.Instance.Players)
        {
            // Ha nincs csapata vagy A csapat → PlayerListA
            // Ha B csapat → PlayerListB
            Transform targetList = GetTargetList(player);

            GameObject row = Instantiate(playerRowPrefab, targetList);
            row.GetComponent<PlayerRowUI>().Setup(player);
        }
    }

    private Transform GetTargetList(Player player)
    {
        if (player.Team == null)
            return playerListA;

        return player.Team.Name == "B" ? playerListB : playerListA;
    }
}