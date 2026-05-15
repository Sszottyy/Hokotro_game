using SnowPlow.Model.Players;
using TMPro;
using UnityEngine;

public class PlayerRowUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI vehiclesText;

    private void Awake()
    {
        if (nameText == null)
            nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();

        if (vehiclesText == null)
            vehiclesText = transform.Find("VehiclesText")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null && nameText.font == null)
        {
            Debug.LogWarning("Fixing missing TMP font on nameText");
            nameText.font = Resources.Load<TMPro.TMP_FontAsset>("ButtonStyle");
        }

        if (vehiclesText != null && vehiclesText.font == null)
        {
            Debug.LogWarning("Fixing missing TMP font on vehiclesText");
            vehiclesText.font = Resources.Load<TMPro.TMP_FontAsset>("ButtonStyle");
        }
    }
    public void Setup(Player player)
    {
        Debug.Log($"Setup hívva. Player neve: '{player.Name}'");
        Debug.Log($"nameText null? {nameText == null}");
        Debug.Log($"vehiclesText null? {vehiclesText == null}");

        if (nameText == null || vehiclesText == null) return;

        nameText.text = player.Name;
        vehiclesText.text = player.Vehicles.Count > 0
            //? player.Vehicles[0].GetType().ToString()
            ? player.Vehicles[0].GetType().Name
            : "Nincs jármű";

        nameText.enabled = true;
        vehiclesText.enabled = true;

        Debug.Log($"nameText.text beállítva: '{nameText.text}'");
        Debug.Log($"vehiclesText.text beállítva: '{vehiclesText.text}'");
    }


    public void Setup(string playerName, PlayerRole role, string teamName)
    {
        Debug.Log($"Setup hívva string paraméterekkel. Player neve: '{playerName}', Role: {role}");

        if (nameText == null || vehiclesText == null) return;

        nameText.text = playerName;

        // Jármű típusának meghatározása a role alapján
        if (role == PlayerRole.SnowPlowDriver)
        {
            vehiclesText.text = "SnowPlow";
        }
        else if (role == PlayerRole.BusDriver)
        {
            vehiclesText.text = "Bus";
        }
        else
        {
            vehiclesText.text = "Nincs jármű";
        }

        nameText.enabled = true;
        vehiclesText.enabled = true;

        Debug.Log($"nameText.text beállítva: '{nameText.text}'");
        Debug.Log($"vehiclesText.text beállítva: '{vehiclesText.text}'");
    }
}