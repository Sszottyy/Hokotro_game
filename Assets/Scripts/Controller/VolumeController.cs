using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Assets.Scripts.Controller
{
    public class VolumeController: MonoBehaviour
    {
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider volumeSlider;

        private void Start()
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);

            // kezdő érték
            volumeSlider.value = 1f;
        }

        public void SetVolume(float value)
        {
            // logaritmikus hangerő
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
        }
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
