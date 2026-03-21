using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// 데이터 테이블 Loader
    /// </summary>
    public class TableLoaderManager : TableLoaderBase
    {
        public static TableLoaderManager Instance;

        public TableNpc TableNpc { get; private set; } = new TableNpc();
        public TableMap TableMap { get; private set; } = new TableMap();
        public TableMonster TableMonster { get; private set; } = new TableMonster();
        public TableAnimation TableAnimation { get; private set; } = new TableAnimation();
        public TableItem TableItem { get; private set; } = new TableItem();
        // Item option tables
        public TableItemBaseOption TableItemBaseOption { get; private set; } = new TableItemBaseOption();
        public TableItemAffixDef TableItemAffixDef { get; private set; } = new TableItemAffixDef();
        public TableItemAffixPool TableItemAffixPool { get; private set; } = new TableItemAffixPool();
        public TableItemRollRule TableItemRollRule { get; private set; } = new TableItemRollRule();
        public TableMonsterDropRate TableMonsterDropRate { get; private set; } = new TableMonsterDropRate();
        public TableNpcDropRate TableNpcDropRate { get; private set; } = new TableNpcDropRate();
        public TableItemDropGroup TableItemDropGroup { get; private set; } = new TableItemDropGroup();
        public TableExp TableExp { get; private set; } = new TableExp();
        public TableWindow TableWindow { get; private set; } = new TableWindow();
        public TableStat TableStat { get; private set; } = new TableStat();
        public TableDamageType TableDamageType { get; private set; } = new TableDamageType();
        public TableState TableState { get; private set; } = new TableState();
        public TableCrowdControl TableCrowdControl { get; private set; } = new TableCrowdControl();
        public TableVfx TableVfx { get; private set; } = new TableVfx();
        public TableInteraction TableInteraction { get; private set; } = new TableInteraction();
        public TableShop TableShop { get; private set; } = new TableShop();
        public TableItemUpgrade TableItemUpgrade { get; private set; } = new TableItemUpgrade();
        public TableItemSalvage TableItemSalvage { get; private set; } = new TableItemSalvage();
        public TableItemCraft TableItemCraft { get; private set; } = new TableItemCraft();
        public TableCutscene TableCutscene { get; private set; } = new TableCutscene();
        public TableDialogue TableDialogue { get; private set; } = new TableDialogue();
        public TableQuest TableQuest { get; private set; } = new TableQuest();
        public TableProjectile TableProjectile { get; private set; } = new TableProjectile();
        public TableSound TableSound { get; private set; } = new TableSound();
        public TableSimulationTool TableSimulationTool { get; private set; } = new TableSimulationTool();
        public TableSimulationGrowth TableSimulationGrowth { get; private set; } = new TableSimulationGrowth();
        public TableItemUse TableItemUse { get; private set; } = new TableItemUse();
        public TableItemUseAction TableItemUseAction { get; private set; } = new TableItemUseAction();
        
        protected void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }

                registry = new TableRegistry();
                registry.Register(TableAnimation);
                registry.Register(TableMonster);
                registry.Register(TableNpc);
                registry.Register(TableMap);
                registry.Register(TableItem);
                registry.Register(TableItemBaseOption);
                registry.Register(TableItemAffixDef);
                registry.Register(TableItemAffixPool);
                registry.Register(TableItemRollRule);
                registry.Register(TableMonsterDropRate);
                registry.Register(TableNpcDropRate);
                registry.Register(TableItemDropGroup);
                registry.Register(TableExp);
                registry.Register(TableWindow);
                registry.Register(TableStat);
                registry.Register(TableDamageType);
                registry.Register(TableState);
                registry.Register(TableCrowdControl);
                registry.Register(TableVfx);
                registry.Register(TableInteraction);
                registry.Register(TableShop);
                registry.Register(TableItemUpgrade);
                registry.Register(TableItemSalvage);
                registry.Register(TableItemCraft);
                registry.Register(TableCutscene);
                registry.Register(TableDialogue);
                registry.Register(TableQuest);
                registry.Register(TableProjectile);
                registry.Register(TableSound);
                registry.Register(TableSimulationTool);
                registry.Register(TableSimulationGrowth);
                registry.Register(TableItemUse);
                registry.Register(TableItemUseAction);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private float GetNpcMoveStep(int npcUid)
        {
            var info = TableNpc.GetDataByUid(npcUid);
            if (info == null) return 0;
            var info2 = GetAnimationData(info.AnimationUid);
            if (info2 is { MoveStep: > 0 })
            {
                return info2.MoveStep;
            }
            return 0;
        }

        private float GetMonsterMoveStep(int monsterUid)
        {
            var info = TableMonster.GetDataByUid(monsterUid);
            if (info == null) return 0;
            var info2 = GetAnimationData(info.AnimationUid);
            if (info2 is { MoveStep: > 0 })
            {
                return info2.MoveStep;
            }
            return 0;
        }

        

        /// <summary>
        /// Locale 변경 등으로 인해, 로드 시점에 캐시된 표시용 Name 필드를 다시 로컬라이즈합니다.
        /// - Stat/DamageType/State 테이블은 로드 시점에 Name을 덮어쓰므로, Locale 변경 시 재적용이 필요합니다.
        /// </summary>
        public void RefreshStatusNames()
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) return;

            TableStat.RefreshLocalizedNames(loc);
            TableDamageType.RefreshLocalizedNames(loc);
            TableState.RefreshLocalizedNames(loc);
        }

        public float GetCharacterMoveStep(CharacterConstants.Type type, int characterUid)
        {
            if (type == CharacterConstants.Type.Npc)
            {
                return GetNpcMoveStep(characterUid);
            }
            else if (type == CharacterConstants.Type.Monster)
            {
                return GetMonsterMoveStep(characterUid);
            }

            return 0;
        }

        // =======================================
        // Facade Accessors for ALL Tables (Get/Try)
        // =======================================

        // Npc
        public StruckTableNpc GetNpcData(int uid, bool logIfMissing = true)
            => GetData(TableNpc, uid, "NPC", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetNpcData(int uid, out StruckTableNpc data, bool logIfMissing = false)
            => TryGetData(TableNpc, uid, out data, "NPC", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Map
        public StruckTableMap GetMapData(int uid, bool logIfMissing = true)
            => GetData(TableMap, uid, "Map", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetMapData(int uid, out StruckTableMap data, bool logIfMissing = false)
            => TryGetData(TableMap, uid, out data, "Map", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Monster
        public StruckTableMonster GetMonsterData(int uid, bool logIfMissing = true)
            => GetData(TableMonster, uid, "Monster", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetMonsterData(int uid, out StruckTableMonster data, bool logIfMissing = false)
            => TryGetData(TableMonster, uid, out data, "Monster", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Animation
        public StruckTableAnimation GetAnimationData(int uid, bool logIfMissing = true)
            => GetData(TableAnimation, uid, "Animation", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetAnimationData(int uid, out StruckTableAnimation data, bool logIfMissing = false)
            => TryGetData(TableAnimation, uid, out data, "Animation", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Item
        public StruckTableItem GetItemData(int uid, bool logIfMissing = true)
            => GetData(TableItem, uid, "Item", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetItemData(int uid, out StruckTableItem data, bool logIfMissing = false)
            => TryGetData(TableItem, uid, out data, "Item", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Window
        public StruckTableWindow GetWindowData(int uid, bool logIfMissing = true)
            => GetData(TableWindow, uid, "Window", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetWindowData(int uid, out StruckTableWindow data, bool logIfMissing = false)
            => TryGetData(TableWindow, uid, out data, "Window", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Vfx
        public StruckTableVfx GetVfxData(int uid, bool logIfMissing = true)
            => GetData(TableVfx, uid, "Vfx", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetVfxData(int uid, out StruckTableVfx data, bool logIfMissing = false)
            => TryGetData(TableVfx, uid, out data, "Vfx", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Interaction
        public StruckTableInteraction GetInteractionData(int uid, bool logIfMissing = true)
            => GetData(TableInteraction, uid, "Interaction", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetInteractionData(int uid, out StruckTableInteraction data, bool logIfMissing = false)
            => TryGetData(TableInteraction, uid, out data, "Interaction", (t, i) => t.GetDataByUid(i), logIfMissing);

        // ItemUpgrade
        public StruckTableItemUpgrade GetItemUpgradeData(int uid, bool logIfMissing = true)
            => GetData(TableItemUpgrade, uid, "ItemUpgrade", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetItemUpgradeData(int uid, out StruckTableItemUpgrade data, bool logIfMissing = false)
            => TryGetData(TableItemUpgrade, uid, out data, "ItemUpgrade", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Cutscene
        public StruckTableCutscene GetCutsceneData(int uid, bool logIfMissing = true)
            => GetData(TableCutscene, uid, "Cutscene", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetCutsceneData(int uid, out StruckTableCutscene data, bool logIfMissing = false)
            => TryGetData(TableCutscene, uid, out data, "Cutscene", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Dialogue
        public StruckTableDialogue GetDialogueData(int uid, bool logIfMissing = true)
            => GetData(TableDialogue, uid, "Dialogue", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetDialogueData(int uid, out StruckTableDialogue data, bool logIfMissing = false)
            => TryGetData(TableDialogue, uid, out data, "Dialogue", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Quest
        public StruckTableQuest GetQuestData(int uid, bool logIfMissing = true)
            => GetData(TableQuest, uid, "Quest", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetQuestData(int uid, out StruckTableQuest data, bool logIfMissing = false)
            => TryGetData(TableQuest, uid, out data, "Quest", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Projectile
        public StruckTableProjectile GetProjectileData(int uid, bool logIfMissing = true)
            => GetData(TableProjectile, uid, "Projectile", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetProjectileData(int uid, out StruckTableProjectile data, bool logIfMissing = false)
            => TryGetData(TableProjectile, uid, out data, "Projectile", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Sound
        public StruckTableSound GetSoundData(int uid, bool logIfMissing = true)
            => GetData(TableSound, uid, "Sound", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetSoundData(int uid, out StruckTableSound data, bool logIfMissing = false)
            => TryGetData(TableSound, uid, out data, "Sound", (t, i) => t.GetDataByUid(i), logIfMissing);

    }
}
