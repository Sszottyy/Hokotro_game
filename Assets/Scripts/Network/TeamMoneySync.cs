using Unity.Netcode;
using UnityEngine;

public class TeamMoneySync : NetworkBehaviour
{
    public static TeamMoneySync Instance;

    private void Awake()
    {
        Instance = this;
    }

    [ClientRpc]
    public void UpdateMoneyClientRpc(
        int teamAMoney,
        int teamBMoney)
    {
        GameManager.Instance.TeamA.SetMoney(teamAMoney);
        GameManager.Instance.TeamB.SetMoney(teamBMoney);
    }

    
    public void SyncMoney()
    {
        if (!IsServer)
            return;
        UpdateMoneyClientRpc(
            GameManager.Instance.TeamA.Money,
            GameManager.Instance.TeamB.Money
        );
    }
}