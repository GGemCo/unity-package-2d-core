using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class UIWindowOption : MonoBehaviour
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        public TMP_Dropdown dropdownLanguage;
        public Button buttonConfirm;
        public Button buttonCancel;

        private void Awake()
        {
            buttonConfirm?.onClick.AddListener(OnClickConfirm);
            buttonCancel?.onClick.AddListener(OnClickClose);
            dropdownLanguage?.onValueChanged.AddListener(OnChangeDropdownLanguage);
        }
        private void Start()
        {
            gameObject.SetActive(false);
            if (dropdownLanguage != null)
            {
                dropdownLanguage.ClearOptions();
                List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
                foreach (LocalizationConstants.LanguageIndex lang in Enum.GetValues(typeof(LocalizationConstants.LanguageIndex)))
                {
                    options.Add(new TMP_Dropdown.OptionData(LocalizationConstants.LanguageNames.GetValueOrDefault(lang)));
                }
                dropdownLanguage.AddOptions(options);
            }
        }

        public void Show(bool show)
        {
            gameObject.SetActive(show);
        }
        private void OnEnable()
        {
            int index = PlayerPrefsManager.LoadIndexLocalizationLocale();
            if (index != -1)
            {
                dropdownLanguage.value = index;
            }
        }

        private void OnChangeDropdownLanguage(int value)
        {
            // Debug.Log($"select: {value}");
        }

        private void OnClickConfirm()
        {
            // Debug.Log($"dropdownLanguage.value: {dropdownLanguage.value}");
            LocalizationManager.Instance?.StartChangeLocale(dropdownLanguage.value);
        }
        private void OnClickClose()
        {
            Show(false);
        }

    }
}