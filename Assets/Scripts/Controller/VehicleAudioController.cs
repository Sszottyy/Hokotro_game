using UnityEngine;

public class VehicleAudioController : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip engineStartClip;
    [SerializeField] private AudioClip idleClip;
    [SerializeField] private AudioClip driveClip;

    [Header("Volumes")]
    [SerializeField] private float startVolume = 1f;
    [SerializeField] private float idleVolume = 1f;
    [SerializeField] private float driveVolume = 1.5f;

    [Header("Settings")]
    [SerializeField] private float moveThreshold = 0.1f; // min speed to count as moving

    private AudioSource audioSource;
    private Rigidbody2D rb;

    private enum EngineState { Starting, Idle, Driving }
    private EngineState currentState;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        PlayStart();
    }

    private void Update()
    {
        // Don't interrupt the start sound
        if (currentState == EngineState.Starting)
        {
            if (!audioSource.isPlaying)
                TransitionTo(EngineState.Idle); // start finished, go to idle
            return;
        }

        bool isMoving = rb.linearVelocity.magnitude > moveThreshold;

        if (isMoving && currentState != EngineState.Driving)
            TransitionTo(EngineState.Driving);
        else if (!isMoving && currentState != EngineState.Idle)
            TransitionTo(EngineState.Idle);
    }

    private void PlayStart()
    {
        currentState = EngineState.Starting;
        audioSource.loop = false;
        audioSource.volume = startVolume;
        audioSource.clip = engineStartClip;
        audioSource.Play();
    }

    private void TransitionTo(EngineState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case EngineState.Idle:
                audioSource.loop = true;
                audioSource.volume = idleVolume;
                audioSource.clip = idleClip;
                audioSource.Play();
                break;

            case EngineState.Driving:
                audioSource.loop = true;
                audioSource.volume = driveVolume;
                audioSource.clip = driveClip;
                audioSource.Play();
                break;
        }
    }
}