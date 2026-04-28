using SnowPlow.Model.Players;
using TMPro;
using UnityEngine;

public class PlayerRowUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI vehiclesText;

    public void Setup(Player player)
    {
        Debug.Log($"Setup hívva. Player neve: '{player.Name}'");
        Debug.Log($"nameText null? {nameText == null}");
        Debug.Log($"vehiclesText null? {vehiclesText == null}");

        if (nameText == null || vehiclesText == null) return;

        nameText.text = player.Name;
        vehiclesText.text = player.Vehicles.Count > 0
            ? player.Vehicles[0].GetType().ToString()
            : "Nincs jármű";

        nameText.enabled = true;
        vehiclesText.enabled = true;

        Debug.Log($"nameText.text beállítva: '{nameText.text}'");
        Debug.Log($"vehiclesText.text beállítva: '{vehiclesText.text}'");
    }
}