using Unity.Netcode;
using UnityEngine;

public class TeamScoreSync : NetworkBehaviour
{
    public static TeamScoreSync Instance;

    private void Awake()
    {
        Instance = this;
    }

    [ClientRpc]
    public void UpdateScoreClientRpc(
        int teamAScore,
        int teamBScore)
    {
        GameManager.Instance.TeamA.SetScore(teamAScore);
        GameManager.Instance.TeamB.SetScore(teamBScore);
    }

    public void SyncScore()
    {
        if (!IsServer)
            return;

        UpdateScoreClientRpc(
            GameManager.Instance.TeamA.Score,
            GameManager.Instance.TeamB.Score
        );
    }
}