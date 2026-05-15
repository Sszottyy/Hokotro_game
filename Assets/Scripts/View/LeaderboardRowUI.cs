using SnowPlow.Model.Players;
using TMPro;
using UnityEngine;

public class LeaderboardRowUI : MonoBehaviour
{
    public TextMeshProUGUI placeText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI teamText;
    public TextMeshProUGUI vehicleText;
    public TextMeshProUGUI scoreText;

    public void Setup(Player player, int place)
    {
        placeText.text = place + ".";
        nameText.text = player.Name;
        teamText.text = player.Team != null ? player.Team.Name : "-";
        vehicleText.text = player.Vehicles.Count > 0
            ? player.Vehicles[0].ToString()
            : "-";

        // Score a csapatból jön, ha van csapata
        scoreText.text = player.Team != null
            ? player.Team.Score.ToString()
            : "0";
    }
}