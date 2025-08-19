using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    ///  기본 설정 
    /// </summary>
    public class UIPanelOptionDefault : UIPanelOptionBase
    {
        private UIWindowOption _windowOption;
        
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("언어 선택 드롭 다운 메뉴")]
        [SerializeField] private TMP_Dropdown dropdownLanguage;
        [Tooltip("메인 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeMaster;
        [Tooltip("BGM 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeBgm;
        [Tooltip("효과음 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeSfx;
        
        [HideInInspector] public GGemCoOptionSettings optionSettings;

        protected override void Awake()
        {
            base.Awake();
            InitializeLanguage();
            
            dropdownLanguage?.onValueChanged.AddListener(OnChangeDropdownLanguage);
            sliderVolumeMaster?.onValueChanged.AddListener(OnChangeSliderMaster);
            sliderVolumeBgm?.onValueChanged.AddListener(OnChangeSliderBgm);
            sliderVolumeSfx?.onValueChanged.AddListener(OnChangeSliderSfx);
            
            if (!AddressableLoaderSettings.Instance) return;
            optionSettings = AddressableLoaderSettings.Instance.optionSettings;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            dropdownLanguage?.onValueChanged.RemoveAllListeners();
            sliderVolumeMaster?.onValueChanged.RemoveAllListeners();
            sliderVolumeBgm?.onValueChanged.RemoveAllListeners();
            sliderVolumeSfx?.onValueChanged.RemoveAllListeners();
        }
        private void OnChangeDropdownLanguage(int value)
        {
            // GcLogger.Log($"select: {value}");
            SetIsChange(true);
        }
        /// <summary>
        /// 메인 볼륨 조절
        /// </summary>
        /// <param name="value"></param>
        private void OnChangeSliderMaster(float value)
        {
            // GcLogger.Log($"bgm volume: {value}");
            if (soundManager)
            {
                soundManager.SetMasterVolume(value, false);
            }

            SetIsChange(true);
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
                soundManager.SetBgmVolume(value, false);
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
            if (soundManager)
            {
                soundManager.SetSfxVolume(value, false);
            }
            SetIsChange(true);
        }

        private void InitializeLanguage()
        {
            if (dropdownLanguage == null) return;
            dropdownLanguage.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (LocalizationConstants.LanguageIndex lang in Enum.GetValues(typeof(LocalizationConstants.LanguageIndex)))
            {
                options.Add(new TMP_Dropdown.OptionData(LocalizationConstants.LanguageNames.GetValueOrDefault(lang)));
            }
            dropdownLanguage.AddOptions(options);
        }

        private void LoadCurrentOptions()
        {
            if (!dropdownLanguage) return;
            
            // 현재 설정된 언어로 dropdownLanguage 셋팅하기
            int index = PlayerPrefsManager.LoadIndexLocalizationLocale();
            if (index != -1)
            {
                dropdownLanguage.SetValueWithoutNotify(index);
            }
            float value = PlayerPrefsManager.LoadSoundVolumeMaster();
            if (sliderVolumeMaster != null)
            {
                sliderVolumeMaster.SetValueWithoutNotify(value);
            }
            value = PlayerPrefsManager.LoadSoundVolumeBGM();
            if (sliderVolumeBgm != null)
            {
                sliderVolumeBgm.SetValueWithoutNotify(value);
            }
            value = PlayerPrefsManager.LoadSoundVolumeSfx();
            if (sliderVolumeSfx != null)
            {
                sliderVolumeSfx.SetValueWithoutNotify(value);
            }
        }

        /// <summary>
        /// 옵션 설정 저장하기
        /// </summary>
        protected override void OnClickConfirm()
        {
            // GcLogger.Log($"dropdownLanguage.value: {dropdownLanguage.value}");
            LocalizationManager.Instance?.StartChangeLocale(dropdownLanguage.value);
            soundManager.SetMasterVolume(sliderVolumeMaster.value);
            soundManager.SetBgmVolume(sliderVolumeBgm.value);
            soundManager.SetSfxVolume(sliderVolumeSfx.value);
            
            // 변경된 항목을 저장하고, _isChanged는 false로 
            SetIsChange(false);
        }
        /// <summary>
        /// 수정한것이 있으면 되돌리기
        /// </summary>
        protected override void OnClickCancel()
        {
            if (!isChanged) return;
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

        private void OnConfirmByPopup()
        {
            LoadCurrentOptions();
            SetButtonInteractable(false);
            // LoadCurrentOptions에서 최신으로 불러오기 때문에, 마지막에 _isChanged를 변경한다.
            SetIsChange(false);
        }

        /// <summary>
        /// 옵션 디폴트 값으로 되돌리기
        /// </summary>
        protected override void OnClickReset()
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
            // GcLogger.Log($"OnConfirmResetByPopup");
            dropdownLanguage.value = (int)LocalizationConstants.DefaultLanguageIndex;
            sliderVolumeMaster.value = optionSettings.volumeMaster;
            sliderVolumeBgm.value = optionSettings.volumeBGM;
            sliderVolumeSfx.value = optionSettings.volumeSfx;
            
            OnClickConfirm();
        }
        public void OnEnable()
        {
            SetIsChange(false);
        }
        /// <summary>
        /// 저장하지 않고 닫을 수 있기 때문에 옵션 창이 닫힐때 현재 설정값 다시 로드
        /// </summary>
        public override bool Show(bool show)
        {
            if (show)
            {
                base.Show(true);
                LoadCurrentOptions();
                return true;
            }
            
            if (!isChanged)
            {
                if (!base.Show(false)) return false;
                return true;
            }
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
            return false;
        }
    }
}