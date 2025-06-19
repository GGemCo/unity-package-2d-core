using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 인트로 씬
    /// </summary>
    public class SceneIntro : MonoBehaviour
    {
        [HideInInspector] public AddressableSettingsLoader addressableSettingsLoader;
        
        [Header("기본오브젝트")]
        [Tooltip("계속하기 버튼")]
        [SerializeField] private Button buttonGameContinue;
        [Tooltip("새로운 게임 버튼")]
        [SerializeField] private Button buttonNewGame;
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

        private SlotMetaDatController slotMetaDatController;
        private GGemCoSaveSettings saveDataSettings;
        private void Awake()
        {
            InitButtons();
            InitializeAddressableSettingLoader();

            if (uIWindowLoadSaveData != null)
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
            addressableSettingsLoader = gameObjectAddressableSettingsLoader.AddComponent<AddressableSettingsLoader>();
            _ = addressableSettingsLoader.InitializeAsync();
            addressableSettingsLoader.OnLoadSettings += InitializeSlotMetaDataManager;
        }

        private void OnDestroy()
        {
            addressableSettingsLoader.OnLoadSettings -= InitializeSlotMetaDataManager;
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
            slotMetaDatController = new SlotMetaDatController(saveSettings.SaveDataFolderName, saveSettings.saveDataMaxSlotCount);
            if (uIWindowLoadSaveData != null)
            {
                uIWindowLoadSaveData.InitializeSaveDataSlots(saveSettings, slotMetaDatController);
            }

            saveDataSettings = saveSettings;

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
            int slotIndex = slotMetaDatController.GetEmptySlotIndex();
            buttonNewGame?.gameObject.SetActive(slotIndex > 0);
            buttonGameContinue?.gameObject.SetActive(PlayerPrefsManager.LoadSaveDataSlotIndex() > 0);
            // slot 데이터가 있는지 채크해서 있으면 buttonOpenSaveDataWindow 버튼 enable 처리 
            buttonOpenSaveDataWindow?.gameObject.SetActive(slotMetaDatController.GetExistSlotCounts() > 0);
        }
        
        /// <summary>
        /// 계속 하기
        /// </summary>
        private void OnClickGameContinue()
        {
            if (saveDataSettings != null && saveDataSettings.UseSaveData)
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
            if (saveDataSettings != null && saveDataSettings.UseSaveData)
            {
                // 남은 슬롯이 있는지 체크
                int slotIndex = slotMetaDatController.GetEmptySlotIndex();
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
