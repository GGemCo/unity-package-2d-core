using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 기본 설정 
    /// </summary>
    public class UIPanelOptionDefault : UIPanelOptionBase
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("언어 선택 드롭 다운 메뉴")]
        [SerializeField] private TMP_Dropdown dropdownLanguage;
        [Tooltip("메인 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeMaster;
        [Tooltip("BGM 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeBgm;
        [Tooltip("효과음 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeSfx;
        
        [Header("버튼")]
        [Tooltip("저장하기")]
        [SerializeField] private Button buttonSave;
        [Tooltip("인트로 씬으로 가기")]
        [SerializeField] private Button buttonGoIntro;
        [Tooltip("종료하기")]
        [SerializeField] private Button buttonExit;
        
        private GGemCoOptionSettings _optionSettings;
        private LocalizationManager _localizationManager;
        // 현재 사용하고 있는 언어 locale
        private Dictionary<string, Locale> _locales;

        protected override void Awake()
        {
            base.Awake();
            if (AddressableLoaderSettings.Instance)
            {
                _optionSettings = AddressableLoaderSettings.Instance.optionSettings;
            }

            if (LocalizationManager.Instance)
            {
                _localizationManager = LocalizationManager.Instance;
                _localizationManager.OnChangeLocale += OnChangeLocale;
                _locales = _localizationManager.GetAvailableLocales();
            }
            InitializeLanguage();
            
            dropdownLanguage?.onValueChanged.AddListener(_ => MarkDirty(true));
            sliderVolumeMaster?.onValueChanged.AddListener(OnMasterVolumeChanged);
            sliderVolumeBgm?.onValueChanged.AddListener(OnBgmVolumeChanged);
            sliderVolumeSfx?.onValueChanged.AddListener(OnSfxVolumeChanged);
            
            buttonSave?.onClick.AddListener(OnGameSave);
            buttonGoIntro?.onClick.AddListener(OnGoIntroScene);
            buttonExit?.onClick.AddListener(OnExit);
        }

        /// <summary>
        /// 각 항목 연결되어있는지 체크
        /// </summary>
        private void OnValidate()
        {
            UIAssertionsChecker.Require(this, dropdownLanguage, nameof(dropdownLanguage));
            UIAssertionsChecker.Require(this, sliderVolumeMaster, nameof(sliderVolumeMaster));
            UIAssertionsChecker.Require(this, sliderVolumeBgm, nameof(sliderVolumeBgm));
            UIAssertionsChecker.Require(this, sliderVolumeSfx, nameof(sliderVolumeSfx));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            dropdownLanguage?.onValueChanged.RemoveAllListeners();
            sliderVolumeMaster?.onValueChanged.RemoveAllListeners();
            sliderVolumeBgm?.onValueChanged.RemoveAllListeners();
            sliderVolumeSfx?.onValueChanged.RemoveAllListeners();
            if (_localizationManager)
            {
                _localizationManager.OnChangeLocale -= OnChangeLocale;
            }
            buttonSave?.onClick.RemoveAllListeners();
            buttonGoIntro?.onClick.RemoveAllListeners();
            buttonExit?.onClick.RemoveAllListeners();
        }
        /// <summary>
        /// 언어 선택 DropDown 만들기
        /// </summary>
        private void InitializeLanguage()
        {
            if (!_localizationManager) return;
            if (_locales.Count == 0)
            {
                dropdownLanguage?.ClearOptions();
                return;
            }
            
            // 표시명 정렬(원하는 정렬 기준으로 변경 가능)
            var options = new List<TMP_Dropdown.OptionData>(_locales.Count);
            foreach (var data in _locales)
                options.Add(new TMP_Dropdown.OptionData(LocalizationConstants.GetName(data.Value)));

            dropdownLanguage.ClearOptions();
            dropdownLanguage.AddOptions(options);
        }
        /// <summary>
        /// 현재 저장되어있는 값으로 다시 셋팅하기
        /// </summary>
        protected override void RefreshFromModel()
        {
            if (!_localizationManager) return;
            // 현재 설정된 언어로 dropdownLanguage 셋팅하기
            string code = PlayerPrefsManager.LoadLocalizationLocaleCode();
            if (!string.IsNullOrEmpty(code))
            {
                int index = _localizationManager.GetLocaleIndexByCode(code);
                dropdownLanguage?.SetValueWithoutNotify(index);
            }
            sliderVolumeMaster?.SetValueWithoutNotify(PlayerPrefsManager.LoadSoundVolumeMaster());
            sliderVolumeBgm?.SetValueWithoutNotify(PlayerPrefsManager.LoadSoundVolumeBGM());
            sliderVolumeSfx?.SetValueWithoutNotify(PlayerPrefsManager.LoadSoundVolumeSfx());
        }
        /// <summary>
        /// 옵션 설정 저장하기
        /// </summary>
        public override bool TryApply()
        {
            // GcLogger.Log($"dropdownLanguage.value: {dropdownLanguage.value}");
            _localizationManager?.StartChangeLocale(dropdownLanguage.value);
            soundManager?.SetMasterVolume(sliderVolumeMaster.value);
            soundManager?.SetBgmVolume(sliderVolumeBgm.value);
            soundManager?.SetSfxVolume(sliderVolumeSfx.value);
            return true;
        }
        /// <summary>
        /// 변경한 것이 있을때, 취소하기
        /// 취소 한 후 언어나 볼륨의 크기도 변경해야 하기 때문에 TryApply 호출
        /// </summary>
        public override void Revert()
        {
            RefreshFromModel();
            TryApply();
        }
        /// <summary>
        /// 디폴트 값으로 되돌리기. 저장하지 않음
        /// </summary>
        protected override void ResetToDefault()
        {
            if (dropdownLanguage)
                _localizationManager?.StartChangeLocale(LocalizationConstants.GetDefaultLocale(), false);

            if (!_optionSettings) return;
            if (sliderVolumeMaster)
                sliderVolumeMaster.value = _optionSettings.volumeMaster;
            if (sliderVolumeBgm)
                sliderVolumeBgm.value = _optionSettings.volumeBGM;
            if (sliderVolumeSfx)
                sliderVolumeSfx.value = _optionSettings.volumeSfx;
        }
        /// <summary>
        /// 메인 볼륨 조절
        /// </summary>
        /// <param name="value"></param>
        private void OnMasterVolumeChanged(float value)
        {
            soundManager?.SetMasterVolume(value, false);
            MarkDirty(true);
        }
        /// <summary>
        /// BGM 볼륨 조절
        /// </summary>
        /// <param name="value"></param>
        private void OnBgmVolumeChanged(float value)
        {
            soundManager?.SetBgmVolume(value, false);
            MarkDirty(true);
        }
        /// <summary>
        /// 효과음 볼륨 조절
        /// </summary>
        /// <param name="value"></param>
        private void OnSfxVolumeChanged(float value)
        {
            soundManager?.SetSfxVolume(value, false);
            MarkDirty(true);
        }
        /// <summary>
        /// 언어 변경이 완료되었을때, DropDown UI 갱신하기
        /// </summary>
        private void OnChangeLocale(string code, int index)
        {
            if (!dropdownLanguage) return;
            dropdownLanguage.value = index;
        }
        /// <summary>
        /// 저장하기
        /// </summary>
        private void OnGameSave()
        {
            if (!SceneGame.Instance) return;
            SceneGame.Instance.saveDataManager.SaveData();
        }
        /// <summary>
        /// 인트로 씬으로 가기
        /// </summary>
        private void OnGoIntroScene()
        {
            if (!SceneGame.Instance) return;
            
            PopupMetadata popupMetadata = new PopupMetadata
            {
                PopupType = PopupManager.Type.Default,
                Title = "System_Game_Exit_Title", // 게임 종료
                Message = "System_Game_Exit", //종료하시겠습니까?\n저장되지 않은 진행 상황을 잃게 됩니다.
                MessageColor = Color.red,
                ShowCancelButton = true,
                OnConfirm = GoToIntroScene,
                IsClosableByClick = false
            };
            popupManager.ShowPopup(popupMetadata);
        }
        private void GoToIntroScene()
        {
            Destroy(SceneGame.Instance.gameObject);
            UnityEngine.SceneManagement.SceneManager.LoadScene(ConfigDefine.SceneNameIntro);
        }
        /// <summary>
        /// 종료하기
        /// </summary>
        private void OnExit()
        {
            PopupMetadata popupMetadata = new PopupMetadata
            {
                PopupType = PopupManager.Type.Default,
                Title = "System_Game_Exit_Title", // 게임 종료
                Message = "System_Game_Exit", //종료하시겠습니까?\n저장되지 않은 진행 상황을 잃게 됩니다.
                MessageColor = Color.red,
                ShowCancelButton = true,
                OnConfirm = ExitGame,
                IsClosableByClick = false
            };
            popupManager.ShowPopup(popupMetadata);
        }

        private void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}