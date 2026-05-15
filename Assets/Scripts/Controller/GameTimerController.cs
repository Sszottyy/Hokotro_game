using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GameTimerController : MonoBehaviour
{
    public float matchDurationSeconds = 300f; // 5 minutes
    public float TimeRemaining { get; private set; }

    public bool IsRunning { get; private set; }

    public event Action OnTimerEnded;

    private void Awake()
    {
        StartTimer();
    }

    public void StartTimer()
    {
        TimeRemaining = matchDurationSeconds;
        IsRunning = true;
    }

    private void Update()
    {
        if (!IsRunning) return;

        TimeRemaining -= Time.deltaTime;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            IsRunning = false;
            OnTimerEnded?.Invoke();
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(TimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(TimeRemaining % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}