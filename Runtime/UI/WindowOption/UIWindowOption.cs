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
        public Button buttonReset;
        public Button buttonCancel;
        
        public Slider sliderBgm;
        public Slider sliderSfx;
        // 변경한 값이 있는지 체크
        private bool _isChanged;
        [Tooltip("팝업 매니저")] [SerializeField] private PopupManager popupManager;
        public void SetPopupManager(PopupManager value) => popupManager = value;
        [Tooltip("사운드 매니저")] [SerializeField] private SoundManager soundManager;
        public void SetSoundManager(SoundManager value) => soundManager = value;
        
        private GGemCoOptionSettings _optionSettings;

        protected override void Awake()
        {
            base.Awake();
            _isChanged = false;
            buttonConfirm?.onClick.AddListener(OnClickConfirm);
            buttonCancel?.onClick.AddListener(OnClickCancel);
            buttonReset?.onClick.AddListener(OnClickReset);

            dropdownLanguage?.onValueChanged.AddListener(OnChangeDropdownLanguage);
            sliderBgm?.onValueChanged.AddListener(OnChangeSliderBgm);
            sliderSfx?.onValueChanged.AddListener(OnChangeSliderSfx);

            SetButtonInteractable(false);
        }

        public void Initialize(GGemCoOptionSettings optionSettings)
        {
            _optionSettings = optionSettings;
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

            if (popupManager == null)
            {
                popupManager = SceneGame.Instance.popupManager;
            }
            if (soundManager == null)
            {
                soundManager = SceneGame.Instance.soundManager;
            }
        }

        private void SetButtonInteractable(bool isInteractable)
        {
            if (buttonConfirm)
            {
                buttonConfirm.interactable = isInteractable;
            }

            if (buttonCancel)
            {
                buttonCancel.interactable = isInteractable;
            }
        }
        /// <summary>
        /// 현재 저장된 정보 불러오기
        /// </summary>
        private void LoadCurrentOptions()
        {
            // 현재 설정된 언어로 dropdownLanguage 셋팅하기
            int index = PlayerPrefsManager.LoadIndexLocalizationLocale();
            if (index != -1)
            {
                dropdownLanguage.value = index;
            }
            float value = PlayerPrefsManager.LoadSoundVolumeBGM();
            if (sliderBgm != null)
            {
                sliderBgm.value = value;
            }
            value = PlayerPrefsManager.LoadSoundVolumeSfx();
            if (sliderSfx != null)
            {
                sliderSfx.value = value;
            }
        }
        private void OnEnable()
        {
            LoadCurrentOptions();
            
            SetIsChange(false);
        }

        private void OnChangeDropdownLanguage(int value)
        {
            // GcLogger.Log($"select: {value}");
            SetIsChange(true);
        }
        /// <summary>
        /// 옵션 설정 저장하기
        /// </summary>
        private void OnClickConfirm()
        {
            // GcLogger.Log($"dropdownLanguage.value: {dropdownLanguage.value}");
            
            LocalizationManager.Instance?.StartChangeLocale(dropdownLanguage.value);
            soundManager.ChangeSoundVolumeBgm(sliderBgm.value);
            soundManager.ChangeSoundVolumeSfx(sliderSfx.value);
            
            SetButtonInteractable(false);
        }
        /// <summary>
        /// 수정한것이 있으면 되돌리기
        /// </summary>
        private void OnClickCancel()
        {
            if (_isChanged)
            {
                PopupMetadata popupMetadata = new PopupMetadata
                {
                    PopupType = PopupManager.Type.Default,
                    MessageColor = Color.red,
                    Title = "취소하기", //슬롯 삭제
                    Message = "변경한 내용을 저장하지 않았습니다.\n취소하시겠습니까?",
                    OnConfirm = OnConfirmByPopup,
                    ShowCancelButton = true
                };
                popupManager.ShowPopup(popupMetadata);
            }
        }

        private void OnConfirmByPopup()
        {
            LoadCurrentOptions();
            SetButtonInteractable(false);
        }
        /// <summary>
        /// BGM 볼륨 조절
        /// </summary>
        /// <param name="value"></param>
        private void OnChangeSliderBgm(float value)
        {
            // GcLogger.Log($"bgm volume: {value}");
            if (soundManager)
            {
                soundManager.ChangeSoundVolumeBgm(value, false);
            }

            SetIsChange(true);
        }
        /// <summary>
        /// 효과음 볼륨 조절
        /// </summary>
        /// <param name="value"></param>
        private void OnChangeSliderSfx(float value)
        {
            // GcLogger.Log($"sfx volume: {value}");
            SetIsChange(true);
        }
        /// <summary>
        /// 변경한 값이 있을 경우 
        /// </summary>
        /// <param name="value"></param>
        private void SetIsChange(bool value)
        {
            _isChanged = value;
            if (value)
            {
                if (buttonConfirm != null)
                {
                    buttonConfirm.interactable = true;
                }
                if (buttonCancel != null)
                {
                    buttonCancel.interactable = true;
                }
            }
            else
            {
                SetButtonInteractable(false);
            }
        }

        /// <summary>
        /// 옵션 디폴트 값으로 되돌리기
        /// </summary>
        private void OnClickReset()
        {
            PopupMetadata popupMetadata = new PopupMetadata
            {
                PopupType = PopupManager.Type.Default,
                MessageColor = Color.red,
                Title = "되돌리기", //슬롯 삭제
                Message = "디폴트 값으로 변경하시겠습니까?",
                OnConfirm = OnConfirmResetByPopup,
                ShowCancelButton = true
            };
            popupManager.ShowPopup(popupMetadata);
        }

        private void OnConfirmResetByPopup()
        {
            GcLogger.Log($"OnConfirmResetByPopup");
            dropdownLanguage.value = (int)LocalizationConstants.DefaultLanguageIndex;
            sliderBgm.value = _optionSettings.volumeBGM;
            sliderSfx.value = _optionSettings.volumeSfx;
            
            OnClickConfirm();
        }
    }
}