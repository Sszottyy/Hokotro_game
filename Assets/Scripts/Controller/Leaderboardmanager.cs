using System.Collections.Generic;
using SnowPlow.Model.Players;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public Transform rowListContent;
    public GameObject leaderboardRowPrefab;

    void OnEnable()
    {
        RefreshLeaderboard();
    }

    public void RefreshLeaderboard()
    {
        Debug.Log("RefreshLeaderboard called");

        ClearList();

        Debug.Log("Players count: " + GameManager.Instance.Players.Count);

        foreach (var p in GameManager.Instance.Players)
        {
            Debug.Log($"Player: {p.Name} Score: {p.Team?.Score}");
        }
        ClearList();

        // Rendezés score szerint csökkenő sorrendben
        List<Player> sorted = new List<Player>(GameManager.Instance.Players);
        sorted.Sort((a, b) =>
        {
            int scoreA = a.Team != null ? a.Team.Score : 0;
            int scoreB = b.Team != null ? b.Team.Score : 0;
            return scoreB.CompareTo(scoreA);
        });

        for (int i = 0; i < sorted.Count; i++)
        {
            GameObject row = Instantiate(leaderboardRowPrefab, rowListContent);
            row.GetComponent<LeaderboardRowUI>().Setup(sorted[i], i + 1);
        }
    }

    private void ClearList()
    {
        foreach (Transform child in rowListContent)
        {
            if (child.gameObject != leaderboardRowPrefab)
                Destroy(child.gameObject);
        }
    }
}