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
        public enum IndexTapButton
        {
            Default,
            Control
        }

        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("탭 토글을 넣을 panel")]
        [SerializeField] private RectTransform panelTabToggle;   // 버튼을 담을 부모(레이아웃 그룹 권장)
        [Tooltip("탭 토글 그룹")]
        [SerializeField] private GameObject toggleGroup;
        [Tooltip("탭 토글 프리팹")]
        [SerializeField] private Toggle prefabTabToggle;
        [Tooltip("디폴트 레이어")]
        [SerializeField] private GameObject panelDefaultLayer;
        
        [Header("기본 옵션 설정")]
        [Tooltip("언어 선택 드롭 다운 메뉴")]
        [SerializeField] private TMP_Dropdown dropdownLanguage;
        [Tooltip("변경한 내용 적용 버튼")]
        [SerializeField] private Button buttonConfirm;
        [Tooltip("디폴트 값으로 초기화 버튼")]
        [SerializeField] private Button buttonReset;
        [Tooltip("변경한 내용 취소 버튼")]
        [SerializeField] private Button buttonCancel;
        
        [Header("사운드 옵션 설정")]
        [Tooltip("메인 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeMaster;
        [Tooltip("BGM 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeBgm;
        [Tooltip("효과음 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeSfx;

        private IndexTapButton _currentIndexTabButton;
        // 변경한 값이 있는지 체크
        private bool _isChanged;
        [Header("매니저")]
        // 인트로 씬에서는 수동으로 넣어주고 있다.
        [Tooltip("팝업 매니저")] [SerializeField] private PopupManager popupManager;
        public void SetPopupManager(PopupManager value) => popupManager = value;
        [Tooltip("사운드 매니저")] [SerializeField] private SoundManager soundManager;
        public void SetSoundManager(SoundManager value) => soundManager = value;
        
        private GGemCoOptionSettings _optionSettings;
        private readonly List<Toggle> _spawned = new();
        private readonly Dictionary<IndexTapButton, GameObject> _dictionaryLayer = new();

        protected override void Awake()
        {
            base.Awake();
            _isChanged = false;
            
            buttonConfirm?.onClick.AddListener(OnClickConfirm);
            buttonCancel?.onClick.AddListener(OnClickCancel);
            buttonReset?.onClick.AddListener(OnClickReset);

            dropdownLanguage?.onValueChanged.AddListener(OnChangeDropdownLanguage);
            sliderVolumeMaster?.onValueChanged.AddListener(OnChangeSliderMaster);
            sliderVolumeBgm?.onValueChanged.AddListener(OnChangeSliderBgm);
            sliderVolumeSfx?.onValueChanged.AddListener(OnChangeSliderSfx);

            SetButtonInteractable(false);
            InitializeTabButton();
            Initialize();
        }

        private void InitializeTabButton()
        {
            if (!panelTabToggle) return;
            if (!toggleGroup) return;

            if (panelDefaultLayer)
            {
                _dictionaryLayer.Add(IndexTapButton.Default, panelDefaultLayer);
            }
            
            // 기존 자식 정리(필요 시)
            for (int i = panelTabToggle.childCount - 1; i >= 0; i--)
                Destroy(panelTabToggle.GetChild(i).gameObject);

            // 열거형 값 배열
            var values = (IndexTapButton[])Enum.GetValues(typeof(IndexTapButton));
            _spawned.Capacity = values.Length;

            foreach (var val in values)
            {
                var go = UIComponentHelper.CreateToggle(prefabTabToggle, val.ToString());
                go.name = $"Btn_{val}";
                go.transform.SetParent(panelTabToggle);
                Toggle toggle = go.GetComponent<Toggle>();
                if (!toggle) continue;
                toggle.group = toggleGroup.GetComponent<ToggleGroup>();
                int captured = (int)val;
                toggle.isOn = false;
                toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                        OnClickTapButton(values[captured]);
                });
                _spawned.Add(toggle);
            }

            // 기본 선택 탭의 시각 상태를 즉시 반영
            foreach (var t in toggleGroup.GetComponentsInChildren<Toggle>())
                t.OnPointerExit(null); // 상태 갱신 트리거 (필요 시)
        }

        private void OnDestroy()
        {
            if (_spawned == null) return;
            // 메모리 누수 방지: 이벤트 해제 및 객체 정리
            foreach (var b in _spawned)
            {
                if (b) b.onValueChanged.RemoveAllListeners();
            }
            _spawned.Clear();
        }
        private void Initialize()
        {
            if (!AddressableLoaderSettings.Instance) return;
            _optionSettings = AddressableLoaderSettings.Instance.optionSettings;
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
            
            // BootstrapperOptionsControls 의 Awake에서 Regist 하고 있다.
            UIWindowOptionsExtensionRegistry uiWindowOptionsExtensionRegistry =
                gameObject.GetComponent<UIWindowOptionsExtensionRegistry>();
            uiWindowOptionsExtensionRegistry?.BuildAll();
            SetIndexTabButton(IndexTapButton.Default);
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
            if (!AddressableLoaderSettings.Instance) return;
            // 현재 설정된 언어로 dropdownLanguage 셋팅하기
            int index = PlayerPrefsManager.LoadIndexLocalizationLocale();
            if (index != -1)
            {
                dropdownLanguage.value = index;
            }
            float value = PlayerPrefsManager.LoadSoundVolumeMaster();
            if (sliderVolumeMaster != null)
            {
                sliderVolumeMaster.value = value;
            }
            value = PlayerPrefsManager.LoadSoundVolumeBGM();
            if (sliderVolumeBgm != null)
            {
                sliderVolumeBgm.value = value;
            }
            value = PlayerPrefsManager.LoadSoundVolumeSfx();
            if (sliderVolumeSfx != null)
            {
                sliderVolumeSfx.value = value;
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
            soundManager.SetMasterVolume(sliderVolumeMaster.value);
            soundManager.SetBgmVolume(sliderVolumeBgm.value);
            soundManager.SetSfxVolume(sliderVolumeSfx.value);
            
            // 변경된 항목을 저장하고, _isChanged는 false로 
            SetIsChange(false);
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
            // LoadCurrentOptions에서 최신으로 불러오기 때문에, 마지막에 _isChanged를 변경한다.
            SetIsChange(false);
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
            // GcLogger.Log($"OnConfirmResetByPopup");
            dropdownLanguage.value = (int)LocalizationConstants.DefaultLanguageIndex;
            sliderVolumeMaster.value = _optionSettings.volumeMaster;
            sliderVolumeBgm.value = _optionSettings.volumeBGM;
            sliderVolumeSfx.value = _optionSettings.volumeSfx;
            
            OnClickConfirm();
        }
        /// <summary>
        /// 저장하지 않고 닫을 수 있기 때문에 옵션 창이 닫힐때 현재 설정값 다시 로드
        /// </summary>
        public override bool Show(bool show)
        {
            if (!show && _isChanged)
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
                return false;
            }
            return base.Show(show);
        }

        private void OnClickTapButton(IndexTapButton tab)
        {
            SetIndexTabButton(tab);
            switch (tab)
            {
                case IndexTapButton.Default:
                    Debug.Log("Default 탭 클릭");
                    break;
                case IndexTapButton.Control:
                    Debug.Log("Control 탭 클릭");
                    break;
                default:
                    Debug.Log($"Unhandled: {tab}");
                    break;
            }
        }

        public void AddLayer(IndexTapButton index, GameObject layer)
        {
            _dictionaryLayer.TryAdd(index, layer);
        }
        private void SetIndexTabButton(IndexTapButton index)
        {
            _currentIndexTabButton = index;
            foreach (var data in _dictionaryLayer)
            {
                data.Value.SetActive(false);
            }

            var selected = _dictionaryLayer.GetValueOrDefault(index);
            if (selected == null) return;
            selected.SetActive(true);
        }

    }
}