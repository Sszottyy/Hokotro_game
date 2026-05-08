using SnowPlow.Model.Players;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public TMP_InputField InputField;
    public Button[] buttons;
    public GameObject endGamePanel;
    public GameObject mainMenuPanel;

    [Header("Új Választó Elemek (3. kép)")]
    public UISwitcher.UISwitcher teamToggle;         // Ha be van kapcsolva = Team B, ha ki = Team A
    public UISwitcher.UISwitcher vehicleToggle;      // Ha be van kapcsolva = Bus, ha ki = Snowplow

    [Header("Panelek")]
    public GameObject hostJoinPanel;  // A Host/Join panel, amit bezárunk
    public GameObject lobbyPanel;

    [Header("Dinamikus Lobby Rendszer (Új!)")]
    public GameObject playerRowPrefab; // A "PlayerRow" Prefab a Project-bõl
    public Transform playerListA;      // A "PlayerListA" GameObject a Hierarchy-ból
    public Transform playerListB;

    public void PlayGame()
    {
        SceneManager.LoadScene("MainGameScene",LoadSceneMode.Single);
    }
    /*public void CreatePlayerInstance()
    {
        string playerName = InputField.text;
        GameManager.Instance.CreatePlayer(playerName);
    }*/

    public void CreatePlayerInstance()
    {
        string playerName = InputField.text;
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "aaa"; // Alapértelmezett név, ha üres lenne
        }

        // Csapat eldöntése a Toggle állása alapján (Toggle ON = Team B, Toggle OFF = Team A)
        string selectedTeam = (teamToggle != null && teamToggle.isOn) ? "Team B" : "Team A";

        // Jármû/Szerepkör eldöntése (Toggle ON = BusDriver, Toggle OFF = SnowPlowDriver)
        PlayerRole selectedRole = (vehicleToggle != null && vehicleToggle.isOn) ? PlayerRole.BusDriver : PlayerRole.SnowPlowDriver;

        // Meghívjuk a GameManager frissített CreatePlayer függvényét
        GameManager.Instance.CreatePlayer(playerName, selectedTeam, selectedRole);

        // Frissítjük a Lobby-ban lévõ szövegeket
        UpdateLobbyUI();

        // Átváltunk a Lobby képernyõre
        if (hostJoinPanel != null) hostJoinPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
    }

    private void UpdateLobbyUI()
    {
        // 1. Elõször kitakarítjuk a régi sorokat a listákból
        CleanList(playerListA);
        CleanList(playerListB);
        

        // 2. Végigmegyünk az összes játékoson, akit a GameManager eltárolt
        foreach (Player p in GameManager.Instance.Players)
        {
            if (p == null) continue;

            // Eldöntjük, melyik listába generáljuk le a sort
            Transform targetParent = (p.Team != null && p.Team.Name == "Team B") ? playerListB : playerListA;

            if (targetParent != null && playerRowPrefab != null)
            {
                // Új sor példányosítása a Prefabból
                GameObject newRow = Instantiate(playerRowPrefab, targetParent);

                // Megkeressük rajta a PlayerRowUI szkriptet, és átadjuk neki a játékost beállításra
                PlayerRowUI rowUI = newRow.GetComponent<PlayerRowUI>();
                if (rowUI != null)
                {
                    rowUI.Setup(p);
                }
            }
        }
    }
    private void CleanList(Transform parent)
    {
        if (parent == null) return;
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    public void CheckInput(string text)
    {
        bool isValid = isValidName(text);
        Debug.Log("Buttos are set to: " + isValid);

        foreach (Button btn in buttons)
        {
            btn.interactable = isValid;
        }

    }
    void Start()
    {
        CheckInput(InputField.text);
        if (GameManager.Instance != null && GameManager.Instance.GameEnded)
        {
            ShowEndScreen();
            GameManager.Instance.GameEnded = false;
        }
        else
        {
            ShowMainMenu();
        }
    }
    public void Quitgame()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }

    private bool isValidName(string text)
    {
        if (text == null || text == "")
            return false;
        return true;
    }

    private void ShowEndScreen()
    {
        endGamePanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    private void ShowMainMenu()
    {
        endGamePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}
