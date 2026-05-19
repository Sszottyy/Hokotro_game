using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Assets.Scripts.Controller
{
    public class VolumeController : MonoBehaviour
    {
        public static VolumeController Instance { get; private set; }

        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider volumeSlider;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            volumeSlider.minValue = 0.0001f;
            volumeSlider.onValueChanged.AddListener(SetVolume);
            volumeSlider.value = 1f;
        }

        public void SetVolume(float value)
        {
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
        }
    }
}
