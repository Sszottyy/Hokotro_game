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


        if (player.Vehicles.Count > 0)
        {
            string vehicleName = player.Vehicles[0].GetType().Name;

            string localizationKey =
                vehicleName.Contains("Bus")
                ? "lobby_bus"
                : "lobby_snowplow";

            vehicleText.text =
                UnityEngine.Localization.Settings.LocalizationSettings
                .StringDatabase
                .GetLocalizedString(
                    "UI_Texts",
                    localizationKey);
        }
        else
        {
            vehicleText.text =
                UnityEngine.Localization.Settings.LocalizationSettings
                .StringDatabase
                .GetLocalizedString(
                    "UI_Texts",
                    "lobby_novehicle");
        }
        // Score a csapatból jön, ha van csapata
        scoreText.text = player.Team != null
            ? player.Team.Score.ToString()
            : "0";
    }
}