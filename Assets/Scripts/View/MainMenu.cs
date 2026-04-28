using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public TMP_InputField InputField;
    public Button[] buttons;
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
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
}
