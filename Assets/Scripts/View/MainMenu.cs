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
    public void PlayGame()
    {
        SceneManager.LoadScene("MainGameScene",LoadSceneMode.Single);
    }
    public void CreatePlayerInstance()
    {
        string playerName = InputField.text;
        GameManager.Instance.CreatePlayer(playerName);
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
