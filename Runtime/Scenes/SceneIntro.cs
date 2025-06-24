using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인트로 씬
    /// </summary>
    public class SceneIntro : MonoBehaviour
    {
        [HideInInspector] public AddressableLoaderSettings addressableLoaderSettings;
        
        [Header("필수 항목")]
        [Tooltip("새로운 게임 버튼")]
        [SerializeField] private Button buttonNewGame;
        public void SetButtonNewGame(Button value) => buttonNewGame = value;
        [Tooltip("계속하기 버튼")]
        [SerializeField] private Button buttonGameContinue;
        public void SetButtonGameContinue(Button value) => buttonGameContinue = value;
        
        [Header("선택 항목")]
        [Tooltip("불러오기 버튼")]
        [SerializeField] private Button buttonOpenSaveDataWindow;
        [Tooltip("옵션 버튼")]
        [SerializeField] private Button buttonOption;
        [Tooltip("게임종료 버튼")]
        [SerializeField] private Button buttonGameExit;
        [Tooltip("불러오기 window")]
        [SerializeField] private UIWindowLoadSaveData uIWindowLoadSaveData;
        [Tooltip("팝업 매니저")]
        [SerializeField] private PopupManager popupManager;

        private SlotMetaDatController _slotMetaDatController;
        private GGemCoSaveSettings _saveDataSettings;
        private void Awake()
        {
            InitButtons();
            InitializeAddressableSettingLoader();

            if (uIWindowLoadSaveData)
            {
                uIWindowLoadSaveData.OnUpdateSlotData += UpdateButtons;
            }
        }
        /// <summary>
        /// GGemCo Settings 파일 읽어오기
        /// </summary>
        private void InitializeAddressableSettingLoader()
        {
            GameObject gameObjectAddressableSettingsLoader = new GameObject("AddressableSettingsLoader");
            addressableLoaderSettings = gameObjectAddressableSettingsLoader.AddComponent<AddressableLoaderSettings>();
            _ = addressableLoaderSettings.InitializeAsync();
            addressableLoaderSettings.OnLoadSettings += InitializeSlotMetaDataManager;
        }

        private void OnDestroy()
        {
            addressableLoaderSettings.OnLoadSettings -= InitializeSlotMetaDataManager;
            buttonGameContinue?.onClick.RemoveAllListeners();
            buttonNewGame?.onClick.RemoveAllListeners();
            buttonOption?.onClick.RemoveAllListeners();
            buttonOpenSaveDataWindow?.onClick.RemoveAllListeners();
            buttonGameExit?.onClick.RemoveAllListeners();
        }
        /// <summary>
        /// 세이븓 데이터 슬롯 정보를 읽어서 버튼 처리 
        /// </summary>
        private void InitializeSlotMetaDataManager(GGemCoSettings settings, GGemCoPlayerSettings playerSettings,
            GGemCoMapSettings mapSettings, GGemCoSaveSettings saveSettings)
        {
            _slotMetaDatController = new SlotMetaDatController(saveSettings.SaveDataFolderName, saveSettings.saveDataMaxSlotCount);
            if (uIWindowLoadSaveData)
            {
                uIWindowLoadSaveData.InitializeSaveDataSlots(saveSettings, _slotMetaDatController);
            }

            _saveDataSettings = saveSettings;

            UpdateButtons();
        }
        /// <summary>
        /// 버튼 초기화. 진행중인 게임이 없을때는 계속하기, 불러오기 버튼은 안보이도록 처리 
        /// </summary>
        private void InitButtons()
        {
            buttonGameContinue?.onClick.AddListener(OnClickGameContinue);
            buttonNewGame?.onClick.AddListener(OnClickNewGame);
            buttonOpenSaveDataWindow?.onClick.AddListener(() => uIWindowLoadSaveData?.Show(true));
            buttonOption?.onClick.AddListener(OnClickOption);
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
                    popupManager.ShowPopupError("선택된 슬롯이 없습니다. 불러오기를 해주세요.");
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
                    GcLogger.LogError("남은 저장 슬롯이 없습니다. 저장되어있는 데이터를 지워주세요.");
                    return;
                }
                // GcLogger.Log("slotindex : " + slotIndex);

                // PlayerPrefs 에 저장하기
                PlayerPrefsManager.SaveSaveDataSlotIndex(slotIndex);
            }

            SceneManager.ChangeScene(ConfigDefine.SceneNameLoading);
        }
        /// <summary>
        /// 옵션
        /// </summary>
        private void OnClickOption()
        {
        }
    }
}
