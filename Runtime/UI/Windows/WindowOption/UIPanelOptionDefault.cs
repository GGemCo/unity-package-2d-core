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
        [Tooltip("언어 선택 UI 정책입니다. Dropdown 또는 Toggle 정책 컴포넌트를 연결합니다.")]
        [SerializeField] private UILanguageSelectionPolicy languageSelectionPolicy;

        [Header("볼륨")]
        [Tooltip("메인 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeMaster;
        [Tooltip("BGM 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeBgm;
        [Tooltip("효과음 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeSfx;
        
        [Header("% 텍스트 오브젝트")]
        [SerializeField] private TMP_Text textPercentMaster;
        [SerializeField] private TMP_Text textPercentBgm;
        [SerializeField] private TMP_Text textPercentSfx;
        
        [Header("버튼")]
        [Tooltip("저장하기")]
        [SerializeField] private Button buttonSave;
        [Tooltip("인트로 씬으로 가기")]
        [SerializeField] private Button buttonGoIntro;
        [Tooltip("종료하기")]
        [SerializeField] private Button buttonExit;
        [Tooltip("게임 진행 데이터만 삭제")]
        [SerializeField] private Button buttonDeleteGameProgressData;
        [Tooltip("앱 로컬 데이터 전체 초기화")]
        [SerializeField] private Button buttonResetAllLocalData;
        
        private GGemCoOptionSettings _optionSettings;
        private LocalizationManager _localizationManager;
        // 현재 사용할 수 있는 언어 Locale 목록입니다.
        private readonly List<Locale> _locales = new();

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
                _locales.Clear();
                foreach (var locale in _localizationManager.GetAvailableLocales().Values)
                {
                    if (locale != null)
                    {
                        _locales.Add(locale);
                    }
                }
            }

            InitializeLanguage();
            
            sliderVolumeMaster?.onValueChanged.AddListener(OnMasterVolumeChanged);
            sliderVolumeBgm?.onValueChanged.AddListener(OnBgmVolumeChanged);
            sliderVolumeSfx?.onValueChanged.AddListener(OnSfxVolumeChanged);
            
            buttonSave?.onClick.AddListener(OnGameSave);
            buttonGoIntro?.onClick.AddListener(OnGoIntroScene);
            buttonExit?.onClick.AddListener(OnExit);
            buttonDeleteGameProgressData?.onClick.AddListener(OnDeleteGameProgressData);
            buttonResetAllLocalData?.onClick.AddListener(OnResetAllLocalData);
        }

        /// <summary>
        /// 각 항목 연결되어있는지 체크
        /// </summary>
        private void OnValidate()
        {
            // UIAssertionsChecker.Require(this, dropdownLanguage, nameof(dropdownLanguage));
            UIAssertionsChecker.Require(this, sliderVolumeMaster, nameof(sliderVolumeMaster));
            UIAssertionsChecker.Require(this, sliderVolumeBgm, nameof(sliderVolumeBgm));
            UIAssertionsChecker.Require(this, sliderVolumeSfx, nameof(sliderVolumeSfx));
            UIAssertionsChecker.Require(this, buttonDeleteGameProgressData, nameof(buttonDeleteGameProgressData));
            UIAssertionsChecker.Require(this, buttonResetAllLocalData, nameof(buttonResetAllLocalData));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnbindLanguageSelectionPolicy();
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
            buttonDeleteGameProgressData?.onClick.RemoveAllListeners();
            buttonResetAllLocalData?.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// 언어 선택 정책을 초기화합니다.
        /// 정책 컴포넌트가 직접 연결되어 있으면 그것을 사용하고, 없으면 기존 Dropdown/Toggle 필드로 호환 정책을 구성합니다.
        /// </summary>
        private void InitializeLanguage()
        {
            if (!_localizationManager) return;

            languageSelectionPolicy = ResolveLanguageSelectionPolicy();
            if (!languageSelectionPolicy)
            {
                GcLogger.LogWarning(this, $"{nameof(UIPanelOptionDefault)} 언어 선택 정책을 찾을 수 없습니다.");
                return;
            }

            languageSelectionPolicy.OnSelectedLocaleChanged -= OnLanguageSelected;
            languageSelectionPolicy.OnSelectedLocaleChanged += OnLanguageSelected;
            languageSelectionPolicy.Initialize(_locales);
        }

        /// <summary>
        /// 사용할 언어 선택 정책 컴포넌트를 찾거나 호환용 정책 컴포넌트를 생성합니다.
        /// 인스펙터에 직접 연결한 정책을 가장 우선하고, 없으면 자식 컴포넌트와 기존 필드 순서로 대체합니다.
        /// </summary>
        /// <returns>초기화할 언어 선택 정책입니다. 사용할 참조가 없으면 null입니다.</returns>
        private UILanguageSelectionPolicy ResolveLanguageSelectionPolicy()
        {
            if (languageSelectionPolicy)
            {
                return languageSelectionPolicy;
            }

            UILanguageSelectionPolicy childPolicy = GetComponentInChildren<UILanguageSelectionPolicy>(true);
            if (childPolicy)
            {
                return childPolicy;
            }

            return null;
        }

        /// <summary>
        /// 언어 선택 정책의 이벤트 연결을 해제합니다.
        /// 패널 파괴 시 정책이 남아 있어도 패널 메서드가 다시 호출되지 않도록 합니다.
        /// </summary>
        private void UnbindLanguageSelectionPolicy()
        {
            if (!languageSelectionPolicy)
            {
                return;
            }

            languageSelectionPolicy.OnSelectedLocaleChanged -= OnLanguageSelected;
        }

        /// <summary>
        /// 사용자가 언어 선택 UI를 조작했을 때 옵션 변경 상태로 표시합니다.
        /// 실제 Locale 적용은 확인 버튼에서 TryApply를 통해 수행합니다.
        /// </summary>
        /// <param name="locale">사용자가 선택한 Locale입니다.</param>
        private void OnLanguageSelected(Locale locale)
        {
            MarkDirty(locale != null);
        }

        /// <summary>
        /// 현재 저장되어있는 값으로 다시 셋팅하기
        /// </summary>
        protected override void RefreshFromModel()
        {
            if (!_localizationManager) return;

            // 현재 설정된 언어로 dropdownLanguage 셋팅하기
            string code = PlayerPrefsManager.LoadLocalizationLocaleCode();
            Locale locale = !string.IsNullOrEmpty(code)
                ? _localizationManager.GetLocaleByCode(code)
                : LocalizationConstants.GetDefaultLocale();

            if (locale == null)
            {
                locale = LocalizationConstants.GetDefaultLocale();
            }

            languageSelectionPolicy?.SetSelectedLocaleWithoutNotify(locale);

            sliderVolumeMaster?.SetValueWithoutNotify(PlayerPrefsManager.LoadSoundVolumeMaster());
            sliderVolumeBgm?.SetValueWithoutNotify(PlayerPrefsManager.LoadSoundVolumeBGM());
            sliderVolumeSfx?.SetValueWithoutNotify(PlayerPrefsManager.LoadSoundVolumeSfx());

            SetTextPercentMaster();
            SetTextPercentBgm();
            SetTextPercentSfx();
        }

        /// <summary>
        /// 옵션 설정 저장하기
        /// </summary>
        public override bool TryApply()
        {
            Locale selectedLocale = languageSelectionPolicy?.GetSelectedLocale();
            if (selectedLocale != null)
            {
                _localizationManager?.StartChangeLocale(selectedLocale);
            }

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
            Locale defaultLocale = LocalizationConstants.GetDefaultLocale();
            if (defaultLocale != null)
            {
                languageSelectionPolicy?.SetSelectedLocaleWithoutNotify(defaultLocale);
                _localizationManager?.StartChangeLocale(defaultLocale, false);
            }

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
            SetTextPercentMaster();
            MarkDirty(true);
        }

        /// <summary>
        /// BGM 볼륨 조절
        /// </summary>
        /// <param name="value"></param>
        private void OnBgmVolumeChanged(float value)
        {
            soundManager?.SetBgmVolume(value, false);
            SetTextPercentBgm();
            MarkDirty(true);
        }

        /// <summary>
        /// 효과음 볼륨 조절
        /// </summary>
        /// <param name="value"></param>
        private void OnSfxVolumeChanged(float value)
        {
            soundManager?.SetSfxVolume(value, false);
            SetTextPercentSfx();
            MarkDirty(true);
        }

        /// <summary>
        /// 언어 변경이 완료되었을 때 현재 정책 UI의 선택 상태를 갱신합니다.
        /// </summary>
        /// <param name="code">변경된 Locale 코드입니다.</param>
        /// <param name="index">변경된 Locale의 목록 인덱스입니다.</param>
        private void OnChangeLocale(string code, int index)
        {
            Locale locale = _localizationManager != null
                ? _localizationManager.GetLocaleByCode(code)
                : GetLocaleByIndex(index);

            languageSelectionPolicy?.SetSelectedLocaleWithoutNotify(locale);
        }

        /// <summary>
        /// 현재 패널이 보유한 Locale 목록에서 인덱스에 해당하는 Locale을 조회합니다.
        /// LocalizationManager에서 코드 조회를 실패했을 때의 보조 경로로 사용합니다.
        /// </summary>
        /// <param name="index">조회할 Locale 인덱스입니다.</param>
        /// <returns>인덱스에 해당하는 Locale입니다. 범위를 벗어나면 null입니다.</returns>
        private Locale GetLocaleByIndex(int index)
        {
            if (index >= 0 && index < _locales.Count)
            {
                return _locales[index];
            }

            return null;
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
                Title = "System_Game_Exit_Title",
                Message = "System_Game_Exit",
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
                Title = "System_Game_Exit_Title",
                Message = "System_Game_Exit",
                MessageColor = Color.red,
                ShowCancelButton = true,
                OnConfirm = ExitGame,
                IsClosableByClick = false
            };
            popupManager.ShowPopup(popupMetadata);
        }

        /// <summary>
        /// 게임 진행 데이터만 삭제 확인 팝업을 엽니다.
        /// </summary>
        private void OnDeleteGameProgressData()
        {
            ShowResetPopup(
                SaveDataResetScope.GameProgressOnly,
                "게임 진행 데이터 삭제",
                "저장 슬롯과 진행 데이터가 모두 삭제됩니다.\n이 작업은 되돌릴 수 없습니다.\n계속하시겠습니까?");
        }

        /// <summary>
        /// 앱 로컬 데이터 전체 초기화 확인 팝업을 엽니다.
        /// </summary>
        private void OnResetAllLocalData()
        {
            ShowResetPopup(
                SaveDataResetScope.AllLocalData,
                "앱 로컬 데이터 전체 초기화",
                "저장 데이터와 로컬 설정이 모두 삭제됩니다.\n이 작업은 되돌릴 수 없습니다.\n계속하시겠습니까?");
        }

        /// <summary>
        /// 초기화 범위에 따른 확인 팝업을 생성합니다.
        /// </summary>
        /// <param name="scope">초기화 범위입니다.</param>
        /// <param name="title">팝업 타이틀입니다.</param>
        /// <param name="message">팝업 메시지입니다.</param>
        private void ShowResetPopup(SaveDataResetScope scope, string title, string message)
        {
            if (popupManager == null)
            {
                return;
            }

            PopupMetadata popupMetadata = new PopupMetadata
            {
                PopupType = PopupManager.Type.Default,
                Title = title,
                Message = message,
                MessageColor = Color.red,
                ShowCancelButton = true,
                IsClosableByClick = false,
                OnConfirm = () => ExecuteLocalDataReset(scope),
            };
            popupManager.ShowPopup(popupMetadata);
        }

        /// <summary>
        /// 선택한 범위대로 로컬 데이터를 초기화합니다.
        /// </summary>
        /// <param name="scope">초기화 범위입니다.</param>
        private void ExecuteLocalDataReset(SaveDataResetScope scope)
        {
            bool success = TryResetLocalData(scope);
            if (!success)
            {
                popupManager?.ShowPopupError("로컬 데이터를 초기화하지 못했습니다.");
                return;
            }

            MoveToIntroSceneAfterReset();
        }

        /// <summary>
        /// 현재 실행 컨텍스트에 맞춰 저장 데이터 초기화 API를 호출합니다.
        /// </summary>
        /// <param name="scope">초기화 범위입니다.</param>
        /// <returns>초기화 성공 여부입니다.</returns>
        private bool TryResetLocalData(SaveDataResetScope scope)
        {
            if (SceneGame.Instance != null && SceneGame.Instance.saveDataManager != null)
            {
                return SceneGame.Instance.saveDataManager.ResetLocalData(scope);
            }

            return SaveDataResetUtility.ResetPersistentStorage(scope);
        }

        /// <summary>
        /// 데이터 초기화 이후 인트로 씬으로 이동합니다.
        /// </summary>
        private void MoveToIntroSceneAfterReset()
        {
            if (SceneGame.Instance != null)
            {
                Destroy(SceneGame.Instance.gameObject);
            }

            SceneManager.ChangeScene(ConfigDefine.SceneNameIntro);
        }

        private void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        private string GetTextPercent(float value)
        {
            return $"{(int)(value * 100)}%";
        }
        
        private void SetTextPercentSfx()
        {
            if (!sliderVolumeSfx || !textPercentSfx) return;
            textPercentSfx.text = GetTextPercent(sliderVolumeSfx.value);
        }

        private void SetTextPercentBgm()
        {
            if (!sliderVolumeBgm || !textPercentBgm) return;
            textPercentBgm.text = GetTextPercent(sliderVolumeBgm.value);
        }

        private void SetTextPercentMaster()
        {
            if (!sliderVolumeMaster || !textPercentMaster) return;
            textPercentMaster.text = GetTextPercent(sliderVolumeMaster.value);
        }
    }
}
