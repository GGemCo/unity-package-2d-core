using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 저장할 데이터 컨테이너 클래스
    /// </summary>
    public class SaveDataContainer
    {
        public PlayerData PlayerData;
        public InventoryData InventoryData;
        public EquipData EquipData;
        public QuestData QuestData;
        public SkillData SkillData;
        public QuickSlotData QuickSlotData;
        public StashData StashData;
    }
    /// <summary>
    /// 세이브 데이터 메인 매니저
    /// </summary>
    public class SaveDataManager : MonoBehaviour
    {
        public PlayerData Player { get; private set; }
        public InventoryData Inventory { get; private set; }
        public EquipData Equip { get; private set; }
        public QuestData Quest { get; private set; }
        public SkillData Skill { get; private set; }
        public QuickSlotData QuickSlot { get; private set; }
        public StashData Stash { get; private set; }
        public ShopSaleData ShopSale { get; private set; }

        private TableLoaderManager tableLoaderManager;
        private SlotMetaDatController slotMetaDatController;
        private SaveFileController saveFileController;
        private ThumbnailController thumbnailController;
        
        // 최대 저장 슬롯 개수
        private int maxSaveSlotCount;
        // 썸네일 width 
        private int thumbnailWidth;
        // 게임 데이터 저장 경로
        private string saveDirectory;
        // 썸네일 저장 경로
        private string thumbnailDirectory;

        // 이 시간안에 저장 요청이 오면 기존 요청은 취소된다.
        private float saveDelay;
        // 강제로 저장할 시간
        private float forceSaveInterval;
        // 마지막 저장된 시간
        private float lastSaveTime;
        // 현재 진행중인 slot index
        private int currentSaveSlot;

        private void Awake()
        {
            tableLoaderManager = TableLoaderManager.Instance;
            if (tableLoaderManager == null) return;

            InitializeSaveDirectory();
            InitializeControllerAndData();
        }
        /// <summary>
        /// 기본 정보를 GGemCo Settings 에서 불러온다.
        /// </summary>
        private void InitializeSaveDirectory()
        {
            GGemCoSaveSettings saveSettings = AddressableSettingsLoader.Instance.saveSettings;
            saveDelay = saveSettings.saveDataDelay;
            forceSaveInterval = saveSettings.saveDataForceSaveInterval;
            thumbnailWidth = saveSettings.saveDataThumbnailWidth;
            maxSaveSlotCount = saveSettings.saveDataMaxSlotCount;

            saveDirectory = saveSettings.SaveDataFolderName;
            thumbnailDirectory = saveSettings.SaveDataThumnailFolderName;
            Directory.CreateDirectory(saveDirectory);
            Directory.CreateDirectory(thumbnailDirectory);
        }
        /// <summary>
        /// 슬롯 관리, 파일 관리, 썸네일 관리 매니저 초기화
        /// </summary>
        private void InitializeControllerAndData()
        {
            slotMetaDatController = new SlotMetaDatController(saveDirectory, maxSaveSlotCount);
            saveFileController = new SaveFileController(saveDirectory, maxSaveSlotCount);
            thumbnailController = new ThumbnailController(thumbnailDirectory, thumbnailWidth);
            // 각 데이터 클래스 초기화
            Player = new PlayerData();
            Inventory = new InventoryData();
            Equip = new EquipData();
            Quest = new QuestData();
            Skill = new SkillData();
            QuickSlot = new QuickSlotData();
            Stash = new StashData();
            ShopSale = new ShopSaleData();

            currentSaveSlot = PlayerPrefsManager.LoadSaveDataSlotIndex();
            
            // 로드한 세이브 데이터 가져오기 
            SaveDataContainer saveDataContainer = SaveDataLoader.Instance.GetSaveDataContainer();

            // 초기화 실행
            Player.Initialize(tableLoaderManager, saveDataContainer);
            Inventory.Initialize(tableLoaderManager, saveDataContainer);
            Equip.Initialize(tableLoaderManager, saveDataContainer);
            Quest.Initialize(tableLoaderManager, saveDataContainer);
            Skill.Initialize(tableLoaderManager, saveDataContainer);
            QuickSlot.Initialize(tableLoaderManager, saveDataContainer);
            Stash.Initialize(tableLoaderManager, saveDataContainer);
            ShopSale.Initialize(tableLoaderManager, saveDataContainer);
        }
        private void Start()
        {
            lastSaveTime = Time.time;
            // 강제 저장 시작 
            InvokeRepeating(nameof(ForceSave), forceSaveInterval, forceSaveInterval);
        }
        /// <summary>
        /// 저장하기 시작
        /// </summary>
        public void StartSaveData()
        {
            // 데이터가 없으면 강제로 한번 저장하기 
            SaveDataContainer saveDataContainer = SaveDataLoader.Instance.GetSaveDataContainer();
            if (saveDataContainer == null)
            {
                SaveData();
            }
            else
            {
                CancelInvoke(nameof(SaveData));
                Invoke(nameof(SaveData), saveDelay);
            }
        }
        /// <summary>
        /// 강제 저장하기
        /// </summary>
        private void ForceSave()
        {
            if (Time.time - lastSaveTime >= forceSaveInterval)
            {
                // GcLogger.Log("강제 저장");
                SaveData();
            }
        }

        /// <summary>
        /// 현재 데이터를 선택한 슬롯에 저장 + 메타파일 업데이트
        /// </summary>
        public void SaveData()
        {
            if (currentSaveSlot < 1 || currentSaveSlot > maxSaveSlotCount)
            {
                GcLogger.LogError("잘못된 슬롯 번호입니다.");
                return;
            }

            string filePath = saveFileController.GetSaveFilePath(currentSaveSlot);
            string thumbnailPath = thumbnailController.GetThumbnailPath(currentSaveSlot);

            Inventory.ClearEmptyInfo();
            Stash.ClearEmptyInfo();
            
            SaveDataContainer saveData = new SaveDataContainer
            {
                PlayerData = Player,
                InventoryData = Inventory,
                EquipData = Equip,
                QuestData = Quest,
                SkillData = Skill,
                QuickSlotData = QuickSlot,
                StashData = Stash,
            };

            string json = JsonConvert.SerializeObject(saveData);
            File.WriteAllText(filePath, json);
            // GcLogger.Log($"데이터가 저장되었습니다. 슬롯 {currentSaveSlot}");
            
            // 썸네일 캡처 후 저장
            if (thumbnailWidth > 0)
            {
                StartCoroutine(thumbnailController.CaptureThumbnail(currentSaveSlot));
            }
            
            // 메타파일 업데이트
            slotMetaDatController.UpdateSlot(currentSaveSlot, thumbnailPath, true, Player.CurrentLevel, filePath);
        }
        /// <summary>
        /// 슬롯 삭제 + 메타파일 업데이트
        /// </summary>
        public void DeleteData(int slot)
        {
            string filePath = saveFileController.GetSaveFilePath(slot);
            string thumbnailPath = thumbnailController.GetThumbnailPath(slot);

            if (File.Exists(filePath)) File.Delete(filePath);
            if (File.Exists(thumbnailPath)) File.Delete(thumbnailPath);
            
            slotMetaDatController.DeleteSlot(slot);
        }

        private void OnDestroy()
        {
            
        }
    }
}