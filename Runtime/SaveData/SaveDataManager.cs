using System.Collections.Generic;
using System.IO;
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
        public SkillData SkillData;
        public QuickSlotData QuickSlotData;
        public QuickSlotSimulationData QuickSlotSimulationData;
        public StashData StashData;
        public GameTimeData GameTimeData;

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
        public SkillData Skill { get; private set; }
        public QuickSlotData QuickSlot { get; private set; }
        public QuickSlotSimulationData QuickSlotSimulation { get; private set; }
        public StashData Stash { get; private set; }
        public ShopSaleData ShopSale { get; private set; }
        public GameTimeData GameTime { get; private set; }

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
            Skill = new SkillData();
            QuickSlot = new QuickSlotData();
            QuickSlotSimulation = new QuickSlotSimulationData();
            Stash = new StashData();
            ShopSale = new ShopSaleData();
            GameTime = new GameTimeData();

            // 인스턴스 아이템 저장소 초기화(테이블 로드 이후면 언제든 사용 가능)
            ItemInstances = new ItemInstanceStore();

            // 초기화 실행
            Player.Initialize(tableLoaderManager, saveDataContainer);
            Inventory.Initialize(tableLoaderManager, saveDataContainer);
            Equip.Initialize(tableLoaderManager, saveDataContainer);
            Quest.Initialize(tableLoaderManager, saveDataContainer);
            Skill.Initialize(tableLoaderManager, saveDataContainer);
            QuickSlot.Initialize(tableLoaderManager, saveDataContainer);
            QuickSlotSimulation.Initialize(tableLoaderManager, saveDataContainer);
            Stash.Initialize(tableLoaderManager, saveDataContainer);
            ShopSale.Initialize(tableLoaderManager, saveDataContainer);
            GameTime.Initialize(tableLoaderManager, saveDataContainer);

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
                SkillData = Skill,
                QuickSlotData = QuickSlot,
                QuickSlotSimulationData = QuickSlotSimulation,
                StashData = Stash,
                GameTimeData = GameTime,
                ItemInstanceStoreData = ItemInstances?.Capture(),
                // 확장 섹션 함께 저장
                Extensions = env?.Sections,
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
            return true;
        }
    }
}