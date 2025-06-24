using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo2DCore
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

        private TableLoaderManager _tableLoaderManager;
        private SlotMetaDatController _slotMetaDatController;
        private SaveFileController _saveFileController;
        private ThumbnailController _thumbnailController;
        
        // 최대 저장 슬롯 개수
        private int _maxSaveSlotCount;
        // 썸네일 width 
        private int _thumbnailWidth;
        // 게임 데이터 저장 경로
        private string _saveDirectory;
        // 썸네일 저장 경로
        private string _thumbnailDirectory;

        // 이 시간안에 저장 요청이 오면 기존 요청은 취소된다.
        private float _saveDelay;
        // 강제로 저장할 시간
        private float _forceSaveInterval;
        // 마지막 저장된 시간
        private float _lastSaveTime;
        // 현재 진행중인 slot index
        private int _currentSaveSlot;
        private bool _useSaveData;

        private void Awake()
        {
            _tableLoaderManager = TableLoaderManager.Instance;
            if (_tableLoaderManager == null) return;

            InitializeSaveDirectory();
            InitializeControllerAndData();
        }
        /// <summary>
        /// 기본 정보를 GGemCo Settings 에서 불러온다.
        /// </summary>
        private void InitializeSaveDirectory()
        {
            GGemCoSaveSettings saveSettings = AddressableLoaderSettings.Instance.saveSettings;
            _useSaveData = saveSettings.UseSaveData;
            _saveDelay = saveSettings.saveDataDelay;
            _forceSaveInterval = saveSettings.saveDataForceSaveInterval;
            _thumbnailWidth = saveSettings.saveDataThumbnailWidth;
            _maxSaveSlotCount = saveSettings.saveDataMaxSlotCount;

            _saveDirectory = saveSettings.SaveDataFolderName;
            _thumbnailDirectory = saveSettings.SaveDataThumnailFolderName;
            Directory.CreateDirectory(_saveDirectory);
            Directory.CreateDirectory(_thumbnailDirectory);
        }
        /// <summary>
        /// 슬롯 관리, 파일 관리, 썸네일 관리 매니저 초기화
        /// </summary>
        private void InitializeControllerAndData()
        {
            _slotMetaDatController = new SlotMetaDatController(_saveDirectory, _maxSaveSlotCount);
            _saveFileController = new SaveFileController(_saveDirectory, _maxSaveSlotCount);
            _thumbnailController = new ThumbnailController(_thumbnailDirectory, _thumbnailWidth);
            // 각 데이터 클래스 초기화
            Player = new PlayerData();
            Inventory = new InventoryData();
            Equip = new EquipData();
            Quest = new QuestData();
            Skill = new SkillData();
            QuickSlot = new QuickSlotData();
            Stash = new StashData();
            ShopSale = new ShopSaleData();

            _currentSaveSlot = PlayerPrefsManager.LoadSaveDataSlotIndex();
            
            // 로드한 세이브 데이터 가져오기 
            SaveDataContainer saveDataContainer = SaveDataLoader.Instance.GetSaveDataContainer();

            // 초기화 실행
            Player.Initialize(_tableLoaderManager, saveDataContainer);
            Inventory.Initialize(_tableLoaderManager, saveDataContainer);
            Equip.Initialize(_tableLoaderManager, saveDataContainer);
            Quest.Initialize(_tableLoaderManager, saveDataContainer);
            Skill.Initialize(_tableLoaderManager, saveDataContainer);
            QuickSlot.Initialize(_tableLoaderManager, saveDataContainer);
            Stash.Initialize(_tableLoaderManager, saveDataContainer);
            ShopSale.Initialize(_tableLoaderManager, saveDataContainer);
        }
        private void Start()
        {
            _lastSaveTime = Time.time;
            // 강제 저장 시작 
            InvokeRepeating(nameof(ForceSave), _forceSaveInterval, _forceSaveInterval);
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
                Invoke(nameof(SaveData), _saveDelay);
            }
        }
        /// <summary>
        /// 강제 저장하기
        /// </summary>
        private void ForceSave()
        {
            if (Time.time - _lastSaveTime >= _forceSaveInterval)
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
            if (!_useSaveData)
            {
                GcLogger.LogWarning($"저장 하기가 비활성화 상태 입니다. {ConfigDefine.NameSDK}SaveSettings 에서 활성화 시켜주세요.");
                return;
            }
            if (_currentSaveSlot < 1 || _currentSaveSlot > _maxSaveSlotCount)
            {
                GcLogger.LogError("잘못된 슬롯 번호입니다.");
                return;
            }

            string filePath = _saveFileController.GetSaveFilePath(_currentSaveSlot);
            string thumbnailPath = _thumbnailController.GetThumbnailPath(_currentSaveSlot);

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
            if (_thumbnailWidth > 0)
            {
                StartCoroutine(_thumbnailController.CaptureThumbnail(_currentSaveSlot));
            }
            
            // 메타파일 업데이트
            _slotMetaDatController.UpdateSlot(_currentSaveSlot, thumbnailPath, true, Player.CurrentLevel, filePath);
        }
        /// <summary>
        /// 슬롯 삭제 + 메타파일 업데이트
        /// </summary>
        public void DeleteData(int slot)
        {
            string filePath = _saveFileController.GetSaveFilePath(slot);
            string thumbnailPath = _thumbnailController.GetThumbnailPath(slot);

            if (File.Exists(filePath)) File.Delete(filePath);
            if (File.Exists(thumbnailPath)) File.Delete(thumbnailPath);
            
            _slotMetaDatController.DeleteSlot(slot);
        }

        private void OnDestroy()
        {
            
        }
    }
}