using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 옵션(설정) 윈도우
    /// </summary>
    public class UIWindowOption : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        public TMP_Dropdown dropdownLanguage;
        public Button buttonConfirm;

        protected override void Awake()
        {
            base.Awake();
            buttonConfirm?.onClick.AddListener(OnClickConfirm);
            dropdownLanguage?.onValueChanged.AddListener(OnChangeDropdownLanguage);
        }
        protected override void Start()
        {
            base.Start();
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

        private void OnEnable()
        {
            // 현재 설정된 언어로 dropdownLanguage 셋팅하기
            int index = PlayerPrefsManager.LoadIndexLocalizationLocale();
            if (index != -1)
            {
                dropdownLanguage.value = index;
            }
        }

        private void OnChangeDropdownLanguage(int value)
        {
            // GcLogger.Log($"select: {value}");
        }
        /// <summary>
        /// 옵션 설정 저장하기
        /// </summary>
        private void OnClickConfirm()
        {
            // GcLogger.Log($"dropdownLanguage.value: {dropdownLanguage.value}");
            LocalizationManager.Instance?.StartChangeLocale(dropdownLanguage.value);
        }
    }
}