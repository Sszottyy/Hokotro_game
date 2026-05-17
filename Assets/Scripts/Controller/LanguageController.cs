using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Assets.Scripts.Controller
{
    class LanguageController : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown dropdown;

        private void Start()
        {
            int savedLanguage = PlayerPrefs.GetInt("language", 0);

            dropdown.value = savedLanguage;

            StartCoroutine(SetLocale(savedLanguage));

            dropdown.onValueChanged.AddListener(ChangeLanguage);
        }

        public void ChangeLanguage(int index)
        {
            StartCoroutine(SetLocale(index));
        }

        IEnumerator SetLocale(int index)
        {
            yield return LocalizationSettings.InitializationOperation;

            LocalizationSettings.SelectedLocale =
                LocalizationSettings.AvailableLocales.Locales[index];

            PlayerPrefs.SetInt("language", index);
        }
    }
}
