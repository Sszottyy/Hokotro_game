using NUnit.Framework;
using SnowPlow.Model.Players;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private Color _defaultInputColor;

    public TMP_InputField InputField;
    public GameObject endGamePanel;
    public GameObject mainMenuPanel;

    public Button hostButton;
    public Button joinButton;
    public Button singlePlayerButton;
    public Button roundaboutConfirmButton;
    public TMP_InputField portInputField;

    [Header("Main Panels")]
    public GameObject mainMenuRoot;
    public GameObject configPanel;

    [Header("Új Választó Elemek (3. kép)")]
    public UISwitcher.UISwitcher teamToggle;         // Ha be van kapcsolva = Team B, ha ki = Team A
    public UISwitcher.UISwitcher vehicleToggle;      // Ha be van kapcsolva = Bus, ha ki = Snowplow

    [SerializeField]
    private TMP_InputField roundaboutInput;

    [Header("Panelek")]
    public GameObject hostJoinPanel;  // A Host/Join panel, amit bezárunk
    public GameObject lobbyPanel;

    [Header("Dinamikus Lobby Rendszer (Új!)")]
    public GameObject playerRowPrefab; // A "PlayerRow" Prefab a Project-bõl
    public Transform playerListA;      // A "PlayerListA" GameObject a Hierarchy-ból
    public Transform playerListB;

    void Awake()
    {
        ColorUtility.TryParseHtmlString("#332CFE", out _defaultInputColor);
        // Csak annyit csináljunk, hogy ellenõrizzük a NetworkObject-ot
        /*NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            Debug.Log($"NetworkObject found on MainMenu. IsSpawned: {netObj.IsSpawned}");
        }
        else
        {
            Debug.LogWarning("No NetworkObject on MainMenu - RPCs won't work from this object");
        }*/
    }
    /*public void PlayGame()
    {
        SceneManager.LoadScene("MainGameScene",LoadSceneMode.Single);
    }*/
    public void StartGameForAll() // ezt kösd a start gombhoz
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Only host can start the game!");
            return;
        }

        // Hálózati szcénakezelõvel töltjük be a jelenetet minden kliensnek
        NetworkManager.Singleton.SceneManager.LoadScene("MainGameScene", LoadSceneMode.Single);
    }
    /*public void CreatePlayerInstance()
    {
        string playerName = InputField.text;
        GameManager.Instance.CreatePlayer(playerName);
    }*/

    public void CreatePlayerInstance()
    {
        Debug.Log("=== CreatePlayerInstance called ===");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager nincs a jelenetben!");
            return;
        }

        Debug.Log($"IsHost: {NetworkManager.Singleton.IsHost}, IsClient: {NetworkManager.Singleton.IsClient}");

        // CSAK akkor engedjük, ha host VAGY client vagyunk
        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("Még nem vagy host vagy client! Elõször indítsd el a StartHost vagy StartClient gombbal!");
            return;
        }
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager nincs a jelenetben!");
            return;
        }
        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("Még nem vagy host vagy client! Elõször indítsd el a StartHost vagy StartClient gombbal!");
            return;
        }
        string playerName = InputField.text;
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "aaa"; // Alapértelmezett név, ha üres lenne
        }

        // Csapat eldöntése a Toggle állása alapján (Toggle ON = Team B, Toggle OFF = Team A)
        string selectedTeam = (teamToggle != null && teamToggle.isOn) ? "Team B" : "Team A";

        // Jármû/Szerepkör eldöntése (Toggle ON = BusDriver, Toggle OFF = SnowPlowDriver)
        PlayerRole selectedRole = (vehicleToggle != null && vehicleToggle.isOn) ? PlayerRole.BusDriver : PlayerRole.SnowPlowDriver;
        Debug.Log($"Creating player: {playerName}, Team: {selectedTeam}, Role: {selectedRole}");
        Debug.Log($"LobbyNetworkHandler.Instance is null? {LobbyNetworkHandler.Instance == null}");
        // Meghívjuk a GameManager frissített CreatePlayer függvényét
        //CreatePlayerInstanceServerRpc(playerName, selectedTeam, selectedRole);
        // GameManager.Instance.CreatePlayer(playerName, selectedTeam, selectedRole);
        if (LobbyNetworkHandler.Instance != null &&
        LobbyNetworkHandler.Instance.IsSpawned)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                int count = 10;

                if (roundaboutInput != null &&
                    !string.IsNullOrEmpty(roundaboutInput.text))
                {
                    int.TryParse(
                        roundaboutInput.text,
                        out count);
                }

                LobbyNetworkHandler.Instance
                    .SetIntersectionCountServerRpc(count);
            }
            Debug.Log($"LobbyNetworkHandler Instance IsSpawned: {LobbyNetworkHandler.Instance.IsSpawned}");
            LobbyNetworkHandler.Instance.CreatePlayerServerRpc(playerName, selectedTeam, selectedRole,
                NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            Debug.LogError("LobbyNetworkHandler.Instance is NULL! Make sure LobbyNetworkHandler GameObject exists in scene!");
            return;
        }
        /*if (IsSpawned)  //  EZT ELLENÕRIZD!
        {
            CreatePlayerInstanceServerRpc(playerName, selectedTeam, selectedRole);
        }
        else
        {
            Debug.LogError("MainMenu nincs spawnolva! Ellenõrizd, hogy van-e rajta NetworkObject komponens!");
            return;
        }*/
        /*if(!IsHost)
        {
            CreatePlayerInstanceServerRpc(playerName, selectedTeam, selectedRole);
        }*/
        // Frissítjük a Lobby-ban lévõ szövegeket
        //UpdateLobbyUI();

        // Átváltunk a Lobby képernyõre
        if (hostJoinPanel != null) hostJoinPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        Debug.Log("=== CreatePlayerInstance finished ===");
    }
    public void UpdateLobbyUIFromData(string name1, string name2, string name3, string name4,
    string team1, string team2, string team3, string team4,
    PlayerRole role1, PlayerRole role2, PlayerRole role3, PlayerRole role4,
    int len)
    {
        // Ugyanaz a kód, mint a ClientRpc-ben
        CleanList(playerListA);
        CleanList(playerListB);

        // 1. játékos
        if (len > 0 && !string.IsNullOrEmpty(name1))
        {
            Transform targetParent = (team1 == "Team B") ? playerListB : playerListA;
            if (targetParent != null && playerRowPrefab != null)
            {
                GameObject newRow = Instantiate(playerRowPrefab, targetParent);
                PlayerRowUI rowUI = newRow.GetComponent<PlayerRowUI>();
                rowUI?.Setup(name1, role1, team1);
            }
        }

        // 2. játékos
        if (len > 1 && !string.IsNullOrEmpty(name2))
        {
            Transform targetParent = (team2 == "Team B") ? playerListB : playerListA;
            if (targetParent != null && playerRowPrefab != null)
            {
                GameObject newRow = Instantiate(playerRowPrefab, targetParent);
                PlayerRowUI rowUI = newRow.GetComponent<PlayerRowUI>();
                rowUI?.Setup(name2, role2, team2);
            }
        }

        // 3. játékos
        if (len > 2 && !string.IsNullOrEmpty(name3))
        {
            Transform targetParent = (team3 == "Team B") ? playerListB : playerListA;
            if (targetParent != null && playerRowPrefab != null)
            {
                GameObject newRow = Instantiate(playerRowPrefab, targetParent);
                PlayerRowUI rowUI = newRow.GetComponent<PlayerRowUI>();
                rowUI?.Setup(name3, role3, team3);
            }
        }

        // 4. játékos
        if (len > 3 && !string.IsNullOrEmpty(name4))
        {
            Transform targetParent = (team4 == "Team B") ? playerListB : playerListA;
            if (targetParent != null && playerRowPrefab != null)
            {
                GameObject newRow = Instantiate(playerRowPrefab, targetParent);
                PlayerRowUI rowUI = newRow.GetComponent<PlayerRowUI>();
                rowUI?.Setup(name4, role4, team4);
            }
        }
        // ... a többi kód ...
    }
    /*[ServerRpc(RequireOwnership = false)]
    public void CreatePlayerInstanceServerRpc(string playerName, string selectedTeam, PlayerRole selectedRole)
    {
        GameManager.Instance.CreatePlayer(playerName, selectedTeam, selectedRole);

        // Frissítjük a lobby UI-t MINDEN kliensen
        string[] name =new string[4];
        string[] team =new string[4];
        PlayerRole[] role =new PlayerRole[4];
        for (int i = 0; i < GameManager.Instance.Players.Count; i++)
        {
            name[i]= GameManager.Instance.Players[i].Name;
            team[i] = GameManager.Instance.Players[i].Team.Name;
            role[i]= GameManager.Instance.Players[i].Role;
        }

        UpdateLobbyUIClientRpc(
        name[0], name[1], name[2], name[3],
        team[0], team[1], team[2], team[3],
        role[0], role[1], role[2], role[3],
        GameManager.Instance.Players.Count
    );

        // Hostnál is frissítsük a helyi UI-t
        UpdateLobbyUI();
    }*/

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
        if (playerListA != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerListA as RectTransform);
        if (playerListB != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerListB as RectTransform);
    }

    [ClientRpc]
    private void UpdateLobbyUIClientRpc(string name1, string name2, string name3, string name4,
    string team1, string team2, string team3, string team4,
    PlayerRole role1, PlayerRole role2, PlayerRole role3, PlayerRole role4,
    int len)
    {
        CleanList(playerListA);
        CleanList(playerListB);

        // 1. játékos
        if (len > 0 && !string.IsNullOrEmpty(name1))
        {
            Transform targetParent = (team1 == "Team B") ? playerListB : playerListA;
            if (targetParent != null && playerRowPrefab != null)
            {
                GameObject newRow = Instantiate(playerRowPrefab, targetParent);
                PlayerRowUI rowUI = newRow.GetComponent<PlayerRowUI>();
                rowUI?.Setup(name1, role1, team1);
            }
        }

        // 2. játékos
        if (len > 1 && !string.IsNullOrEmpty(name2))
        {
            Transform targetParent = (team2 == "Team B") ? playerListB : playerListA;
            if (targetParent != null && playerRowPrefab != null)
            {
                GameObject newRow = Instantiate(playerRowPrefab, targetParent);
                PlayerRowUI rowUI = newRow.GetComponent<PlayerRowUI>();
                rowUI?.Setup(name2, role2, team2);
            }
        }

        // 3. játékos
        if (len > 2 && !string.IsNullOrEmpty(name3))
        {
            Transform targetParent = (team3 == "Team B") ? playerListB : playerListA;
            if (targetParent != null && playerRowPrefab != null)
            {
                GameObject newRow = Instantiate(playerRowPrefab, targetParent);
                PlayerRowUI rowUI = newRow.GetComponent<PlayerRowUI>();
                rowUI?.Setup(name3, role3, team3);
            }
        }

        // 4. játékos
        if (len > 3 && !string.IsNullOrEmpty(name4))
        {
            Transform targetParent = (team4 == "Team B") ? playerListB : playerListA;
            if (targetParent != null && playerRowPrefab != null)
            {
                GameObject newRow = Instantiate(playerRowPrefab, targetParent);
                PlayerRowUI rowUI = newRow.GetComponent<PlayerRowUI>();
                rowUI?.Setup(name4, role4, team4);
            }
        }
        if (playerListA != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerListA as RectTransform);
        if (playerListB != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerListB as RectTransform);
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
        bool nameValid = isValidName(InputField.text);
        bool portValid = isValidName(portInputField != null ? portInputField.text : "");
        bool roundaboutValid = isValidRoundaboutCount(roundaboutInput != null ? roundaboutInput.text : "");

        if (hostButton != null)
            hostButton.interactable = nameValid;

        if (joinButton != null)
            joinButton.interactable = nameValid && portValid;

        if (singlePlayerButton != null)
            singlePlayerButton.interactable = nameValid;

        Debug.Log($"Name valid: {nameValid}, Port valid: {portValid}");
    }

    public void CheckPortInput(string text)
    {
        CheckInput(InputField.text); // reuse the same unified check
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
        MusicManager.Instance?.PlayEndMusic();
    }

    private void ShowMainMenu()
    {
        endGamePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        MusicManager.Instance?.PlayMenuMusic();
    }
    public void ReturnToMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (hostJoinPanel != null)
            hostJoinPanel.SetActive(false);

        if (configPanel != null)
            configPanel.SetActive(false);

        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        MusicManager.Instance?.PlayMenuMusic();

        Debug.Log("[MENU] Returned to main menu");
    }

    private bool isValidRoundaboutCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return true; // empty = use default, that's fine
        if (!int.TryParse(text, out int value)) return false;
        return value >= 5 && value <= 100;
    }

    public void CheckRoundaboutInput(string text)
    {
        string cleaned = System.Text.RegularExpressions.Regex.Replace(text, @"[^0-9]", "");
        if (cleaned != text)
        {
            roundaboutInput.SetTextWithoutNotify(cleaned);
            text = cleaned;
        }

        bool roundaboutValid = isValidRoundaboutCount(text);

        // Red if invalid, default if valid or empty
        roundaboutInput.textComponent.color = roundaboutValid ? _defaultInputColor : Color.red;

        if (roundaboutConfirmButton != null)
            roundaboutConfirmButton.interactable = roundaboutValid;
    }

    public void OnRoundaboutInputEndEdit(string text)
    {
        // Just re-run the visual check when focus is lost, no clamping
        CheckRoundaboutInput(text);
    }
}
