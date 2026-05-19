using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private AudioClip endGameMusic;
    [SerializeField] private float fadeDuration = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainGameScene")
            PlayGameMusic();
        // MainMenu music is handled by ShowMainMenu/ShowEndScreen
    }

    public void PlayMenuMusic()
    {
        StopAllCoroutines();
        StartCoroutine(FadeTo(menuMusic));
    }

    public void PlayGameMusic()
    {
        StopAllCoroutines();
        StartCoroutine(FadeTo(gameMusic));
    }

    public void PlayEndMusic()
    {
        StopAllCoroutines();
        StartCoroutine(FadeTo(endGameMusic));
    }

    private System.Collections.IEnumerator FadeTo(AudioClip clip)
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) yield break;
        if (audioSource.clip == clip && audioSource.isPlaying) yield break;

        float targetVol = 1f;

        if (audioSource.isPlaying)
        {
            float startVol = audioSource.volume;
            while (audioSource.volume > 0)
            {
                float delta = Mathf.Min(Time.unscaledDeltaTime, 0.05f); // clamp spike frames
                audioSource.volume -= startVol * delta / fadeDuration;
                yield return null;
            }
        }

        audioSource.volume = 0f;
        audioSource.clip = clip;
        audioSource.Play();

        while (audioSource.volume < targetVol)
        {
            float delta = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            audioSource.volume += targetVol * delta / fadeDuration;
            yield return null;
        }

        audioSource.volume = targetVol;
    }
}