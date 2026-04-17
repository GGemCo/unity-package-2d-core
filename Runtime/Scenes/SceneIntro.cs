using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인트로 씬
    /// </summary>
    public class SceneIntro : MonoBehaviour
    {
        public string GetFieldNameSceneIntro() => nameof(SceneIntro);
        
        [Header(ConfigCommon.TitleHeaderRequired)]
        [Tooltip("새 게임 버튼")]
        [SerializeField] private Button buttonNewGame;
        public void SetButtonNewGame(Button value) => buttonNewGame = value;
        public string GetFieldNameButtonNewGame() => nameof(buttonNewGame);
        [Tooltip("계속하기 버튼")]
        [SerializeField] private Button buttonGameContinue;
        public void SetButtonGameContinue(Button value) => buttonGameContinue = value;
        public string GetFieldNameButtonGameContinue() => nameof(buttonGameContinue);
        [Tooltip("옵션 버튼")]
        [SerializeField] private Button buttonOpenOption;
        public void SetButtonOption(Button value) => buttonOpenOption = value;
        public string GetFieldNameButtonOption() => nameof(buttonOpenOption);
        [Tooltip("팝업 매니저")]
        [SerializeField] private PopupManager popupManager;
        public void SetPopupManager(PopupManager value) => popupManager = value;
        public string GetFieldNamePopupManager() => nameof(PopupManager);
        [Tooltip("옵션 window")]
        [SerializeField] private UIWindowOption uiWindowOption;
        public void SetUIWindowOption(UIWindowOption value) => uiWindowOption = value;
        public string GetNameUIWindowOption() => nameof(UIWindowOption);
        [Tooltip("사운드 매니저")]
        [SerializeField] private SoundManager soundManager;
        public void SetSoundManager(SoundManager value) => soundManager = value;
        public string GetFieldNameSoundManager() => nameof(SoundManager);
        
        [Header(ConfigCommon.TitleHeaderOption)]
        [Tooltip("불러오기 버튼")]
        [SerializeField] private Button buttonOpenSaveDataWindow;
        public void SetButtonOpenSaveDataWindow(Button value) => buttonOpenSaveDataWindow = value;
        public string GetFieldNameButtonOpenSaveDataWindow() => nameof(buttonOpenSaveDataWindow);
        [Tooltip("게임종료 버튼")]
        [SerializeField] private Button buttonGameExit;
        [Tooltip("불러오기 window")]
        [SerializeField] private UIWindowLoadSaveData uIWindowLoadSaveData;
        public void SetUIWindowLoadSaveData(UIWindowLoadSaveData value) => uIWindowLoadSaveData = value;
        public string GetNameUIWindowLoadSaveData() => nameof(UIWindowLoadSaveData);

        [Tooltip("체크시 바로 게임 시작")]
        [SerializeField] private bool autoStart = false;

        private SlotMetaDatController _slotMetaDatController;
        private GGemCoSaveSettings _saveDataSettings;
        private GameLoaderManager _gameLoaderManager;
        private void Awake()
        {
            if (AddressableLoaderSettings.Instance == null)
            {
                SceneManager.ChangeScene(ConfigDefine.SceneNamePreIntro);
                return;
            }
            var saveSettings = AddressableLoaderSettings.Instance.saveSettings;
            _slotMetaDatController = new SlotMetaDatController(saveSettings.SaveDataFolderName, saveSettings.saveDataMaxSlotCount);
            _saveDataSettings = saveSettings;
            
            InitButtons();

            if (uIWindowLoadSaveData)
            {
                uIWindowLoadSaveData.slotMetaDatController = _slotMetaDatController;
                uIWindowLoadSaveData.OnUpdateSlotData += UpdateButtons;
            }
        }

        private void Start()
        {
            if (autoStart)
            {
                buttonGameContinue?.gameObject.SetActive(false);
                buttonGameExit?.gameObject.SetActive(false);
                buttonOpenSaveDataWindow?.gameObject.SetActive(false);
                buttonOpenOption?.gameObject.SetActive(false);
                buttonNewGame?.gameObject.SetActive(false);
                
                if (_saveDataSettings && _saveDataSettings.UseSaveData)
                {
                    // PlayerPrefs 에서 가져온 값이 있는지 체크 
                    if (PlayerPrefsManager.LoadSaveDataSlotIndex() <= 0)
                    {
                        OnClickNewGame();
                    }
                    else
                    {
                        OnClickGameContinue();
                    }
                }
                else
                {
                    OnClickNewGame();
                }
                return;
            }
            // UI 버튼 활성화
            UpdateButtons();

            // BGM 재생
            // SoundManager Awake 에서 bgm controller 가 생성된다.
            soundManager.PlayBgmIntro();
            // 인트로 SFX Pool 초기화
            soundManager.InitializeSoundSfxPoolForIntro();
        }
        private void OnDestroy()
        {
            buttonGameContinue?.onClick.RemoveAllListeners();
            buttonNewGame?.onClick.RemoveAllListeners();
            buttonOpenOption?.onClick.RemoveAllListeners();
            buttonOpenSaveDataWindow?.onClick.RemoveAllListeners();
            buttonGameExit?.onClick.RemoveAllListeners();
        }
        /// <summary>
        /// 버튼 초기화. 진행중인 게임이 없을때는 계속하기, 불러오기 버튼은 안보이도록 처리 
        /// </summary>
        private void InitButtons()
        {
            buttonGameContinue?.onClick.AddListener(OnClickGameContinue);
            buttonNewGame?.onClick.AddListener(OnClickNewGame);
            buttonOpenSaveDataWindow?.onClick.AddListener(() => uIWindowLoadSaveData?.Show(true));
            buttonOpenOption?.onClick.AddListener(() => uiWindowOption?.Show(true));
            buttonGameExit?.onClick.AddListener(Application.Quit);

            // 진행중인 게임이 없을때 
            if (PlayerPrefsManager.LoadSaveDataSlotIndex() <= 0)
            {
                buttonGameContinue?.gameObject.SetActive(false);
                buttonOpenSaveDataWindow?.gameObject.SetActive(false);
            }
        }
        private void UpdateButtons()
        {
            buttonOpenOption?.gameObject.SetActive(true);
            buttonGameExit?.gameObject.SetActive(true);
            
            // 남은 슬롯 index 채크해서 없으면 buttonNewGame 버튼 disable 처리 
            int slotIndex = _slotMetaDatController.GetEmptySlotIndex();
            if (_saveDataSettings.UseSaveData)
            {
                buttonNewGame?.gameObject.SetActive(slotIndex > 0);
            }
            else
            {
                buttonNewGame?.gameObject.SetActive(true);
            }

            buttonGameContinue?.gameObject.SetActive(PlayerPrefsManager.LoadSaveDataSlotIndex() > 0);
            // slot 데이터가 있는지 채크해서 있으면 buttonOpenSaveDataWindow 버튼 enable 처리 
            buttonOpenSaveDataWindow?.gameObject.SetActive(_slotMetaDatController.GetExistSlotCounts() > 0);
        }
        
        /// <summary>
        /// 계속 하기
        /// </summary>
        private void OnClickGameContinue()
        {
            if (_saveDataSettings && _saveDataSettings.UseSaveData)
            {
                // PlayerPrefs 에서 가져온 값이 있는지 체크 
                if (PlayerPrefsManager.LoadSaveDataSlotIndex() <= 0)
                {
                    popupManager.ShowPopupError("SaveSlot_NotSelected");//"선택된 슬롯이 없습니다.\n불러오기를 해주세요."
                    return;
                }
                // GcLogger.Log("currentSaveDataSlotIndex: " + currentSaveDataSlotIndex);
            }
            
            SceneManager.ChangeScene(ConfigDefine.SceneNameLoading);
        }
        /// <summary>
        /// 새로운 게임
        /// </summary>
        private void OnClickNewGame()
        {
            if (_saveDataSettings && _saveDataSettings.UseSaveData)
            {
                // 남은 슬롯이 있는지 체크
                int slotIndex = _slotMetaDatController.GetEmptySlotIndex();
                if (slotIndex <= 0)
                {
                    popupManager.ShowPopupError("SaveSlot_NoEmptySlot");//"남은 저장 슬롯이 없습니다.\n저장되어있는 데이터를 지워주세요."
                    // GcLogger.LogError("남은 저장 슬롯이 없습니다. 저장되어있는 데이터를 지워주세요.");
                    return;
                }
                // GcLogger.Log("slotindex : " + slotIndex);

                // PlayerPrefs 에 저장하기
                PlayerPrefsManager.SaveSaveDataSlotIndex(slotIndex);
            }

            SceneManager.ChangeScene(ConfigDefine.SceneNameLoading);
        }
    }
}
