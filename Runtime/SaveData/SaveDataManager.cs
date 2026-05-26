using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public QuickSlotData QuickSlotData;
        public QuickSlotSimulationData QuickSlotSimulationData;
        public StashData StashData;
        public GameTimeData GameTimeData;
        public ShopPurchaseData ShopPurchaseData;
        public ShopExposureData ShopExposureData;
        public WindowSlotActivationSaveData WindowSlotActivationSaveData;

        /// <summary>
        /// 맵 클리어 기록, 월드맵 노드 표시 상태, 월드맵 노드 활성 상태를 저장하는 진행 데이터입니다.
        /// </summary>
        public MapProgressData MapProgressData;

        /// <summary>
        /// Key 기반 게임 진행 상태를 저장하는 라이센스 데이터입니다.
        /// </summary>
        public LicenseData LicenseData;

        /// <summary>
        /// 인스턴스 아이템(랜덤 옵션 등) 저장 데이터.
        /// </summary>
        public ItemInstanceStoreData ItemInstanceStoreData;
        
        public Dictionary<string, JToken> Extensions;
    }
    /// <summary>
    /// 세이브 데이터 메인 매니저
    /// </summary>
    public class SaveDataManager : SaveDataManagerBase
    {
        public PlayerData Player { get; private set; }
        public InventoryData Inventory { get; private set; }
        public EquipData Equip { get; private set; }
        public QuestData Quest { get; private set; }
        public QuickSlotData QuickSlot { get; private set; }
        public QuickSlotSimulationData QuickSlotSimulation { get; private set; }
        public StashData Stash { get; private set; }
        public ShopSaleData ShopSale { get; private set; }
        public ShopPurchaseData ShopPurchase { get; private set; }
        public ShopExposureData ShopExposure { get; private set; }
        public GameTimeData GameTime { get; private set; }
        public WindowSlotActivationSaveData WindowSlotActivation { get; private set; }

        /// <summary>
        /// 실제 게임 맵 진행도와 월드맵 노드 표시/활성 상태를 보관하는 저장 데이터입니다.
        /// </summary>
        public MapProgressData MapProgress { get; private set; }

        /// <summary>
        /// 맵 클리어, 월드맵 노드 표시, 월드맵 노드 활성 처리를 담당하는 컨트롤러입니다.
        /// </summary>
        public MapProgressController MapProgressController { get; private set; }

        /// <summary>
        /// Key 기반 라이센스 상태를 설정하고 조회하는 매니저입니다.
        /// </summary>
        public LicenseManager LicenseManager { get; private set; }

        /// <summary>
        /// 세이브 파일에 저장되는 라이센스 상태 데이터입니다.
        /// </summary>
        public LicenseData License { get; private set; }

        /// <summary>
        /// 인스턴스 아이템(랜덤 옵션 등) 저장소.
        /// </summary>
        public ItemInstanceStore ItemInstances { get; private set; }

        /// <summary>
        /// 슬롯 관리, 파일 관리, 썸네일 관리 매니저 초기화
        /// </summary>
        protected override void InitializeData()
        {
            // 로드한 세이브 데이터 가져오기 
            SaveDataContainer saveDataContainer = SaveDataLoader.Instance.GetSaveDataContainer();
            // 각 데이터 클래스 초기화
            Player = new PlayerData();
            Inventory = new InventoryData();
            Equip = new EquipData();
            Quest = new QuestData();
            QuickSlot = new QuickSlotData();
            QuickSlotSimulation = new QuickSlotSimulationData();
            Stash = new StashData();
            ShopSale = new ShopSaleData();
            ShopPurchase = new ShopPurchaseData();
            ShopExposure = new ShopExposureData();
            GameTime = new GameTimeData();
            WindowSlotActivation = new WindowSlotActivationSaveData();
            MapProgress = new MapProgressData();
            MapProgressController = new MapProgressController(this);
            License = new LicenseData();
            LicenseManager = new LicenseManager(this);

            // 인스턴스 아이템 저장소 초기화(테이블 로드 이후면 언제든 사용 가능)
            ItemInstances = new ItemInstanceStore();

            // 초기화 실행
            Player.Initialize(this, tableLoaderManager, saveDataContainer);
            Inventory.Initialize(tableLoaderManager, saveDataContainer);
            Equip.Initialize(tableLoaderManager, saveDataContainer);
            Quest.Initialize(tableLoaderManager, saveDataContainer);
            QuickSlot.Initialize(tableLoaderManager, saveDataContainer);
            QuickSlotSimulation.Initialize(tableLoaderManager, saveDataContainer);
            Stash.Initialize(tableLoaderManager, saveDataContainer);
            ShopSale.Initialize(tableLoaderManager, saveDataContainer);
            ShopPurchase.Initialize(tableLoaderManager, saveDataContainer);
            ShopExposure.Initialize(tableLoaderManager, saveDataContainer);
            GameTime.Initialize(tableLoaderManager, saveDataContainer);
            WindowSlotActivation.Initialize(tableLoaderManager, saveDataContainer);
            MapProgress.Initialize(tableLoaderManager, saveDataContainer);
            License.Initialize(tableLoaderManager, saveDataContainer);
            SceneGame.Instance?.uIWindowManager?.RefreshWindowSlotActivationStates();
            SceneGame.Instance?.uIWindowManager
                ?.GetUIWindowByUid<UIWindowWorldMap>(UIWindowConstants.WindowUid.WorldMap)
                ?.RefreshWorldMapProgressStates();

            // 인스턴스 아이템 복원
            ItemInstances.Restore(saveDataContainer?.ItemInstanceStoreData);
            
            // 외부 섹션 복원
            if (saveDataContainer?.Extensions != null)
            {
                var env = new SaveEnvelope();
                foreach (var kv in saveDataContainer.Extensions)
                    env.Sections[kv.Key] = kv.Value;

                // 순서와 무관하게 복원 보장
                SaveRegistry.ApplyRestore(env);
            }

            // 저장 파일이 없거나 복구할 수 없으면 기본 데이터로 초기화된 현재 상태를 즉시 저장합니다.
            if (SaveDataLoader.Instance.LastLoadResult?.RequiresNewData == true)
            {
                StartSaveData();
            }
        }
        
        /// <summary>
        /// 현재 데이터를 선택한 슬롯에 저장 + 메타파일 업데이트
        /// </summary>
        public override bool SaveData()
        {
            if (!base.SaveData()) return false;
            
            string filePath = saveFileController.GetSaveFilePath(currentSaveSlot);
            string thumbnailPath = thumbnailController.GetThumbnailPath(currentSaveSlot);

            Inventory.ClearEmptyInfo();
            Stash.ClearEmptyInfo();

            // 외부 기여자에게 현재 상태 캡처 요청
            var env = BuildEnvelopeForSave();
            
            SaveDataContainer saveData = new SaveDataContainer
            {
                PlayerData = Player,
                InventoryData = Inventory,
                EquipData = Equip,
                QuestData = Quest,
                QuickSlotData = QuickSlot,
                QuickSlotSimulationData = QuickSlotSimulation,
                StashData = Stash,
                GameTimeData = GameTime,
                ShopPurchaseData = ShopPurchase,
                ShopExposureData = ShopExposure,
                WindowSlotActivationSaveData = WindowSlotActivation,
                MapProgressData = MapProgress,
                LicenseData = License,
                ItemInstanceStoreData = ItemInstances?.Capture(),
                // 확장 섹션 함께 저장
                Extensions = env?.Sections,
            };

            string json = JsonConvert.SerializeObject(saveData);
            saveFileController.WriteSaveJsonWithBackup(currentSaveSlot, json, SaveDataIdentity.Core(currentSaveSlot));
            // GcLogger.Log($"데이터가 저장되었습니다. 슬롯 {currentSaveSlot}");
            
            // 썸네일 캡처 후 저장
            if (thumbnailWidth > 0)
            {
                StartCoroutine(thumbnailController.CaptureThumbnail(currentSaveSlot));
            }
            
            // 메타파일 업데이트
            slotMetaDatController.UpdateSlot(currentSaveSlot, thumbnailPath, true, Player.CurrentLevel, filePath);
            return true;
        }
    }
}
