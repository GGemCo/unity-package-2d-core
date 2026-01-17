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
        public TableMonsterDropRate TableMonsterDropRate { get; private set; } = new TableMonsterDropRate();
        public TableNpcDropRate TableNpcDropRate { get; private set; } = new TableNpcDropRate();
        public TableItemDropGroup TableItemDropGroup { get; private set; } = new TableItemDropGroup();
        public TableExp TableExp { get; private set; } = new TableExp();
        public TableWindow TableWindow { get; private set; } = new TableWindow();
        // Legacy(Deprecated): Status 단일 테이블
        public TableStatus TableStatus { get; private set; } = new TableStatus();
        // New: Status 3분리
        public TableStat TableStat { get; private set; } = new TableStat();
        public TableDamageType TableDamageType { get; private set; } = new TableDamageType();
        public TableState TableState { get; private set; } = new TableState();
        public TableSkill TableSkill { get; private set; } = new TableSkill();
        public TableAffect TableAffect { get; private set; } = new TableAffect();
        public TableAffectModifier TableAffectModifier { get; private set; } = new TableAffectModifier();
        public TableEffect TableEffect { get; private set; } = new TableEffect();
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
        
        protected void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                registry = new TableRegistry();
                registry.Register(TableAnimation);
                registry.Register(TableMonster);
                registry.Register(TableNpc);
                registry.Register(TableMap);
                registry.Register(TableItem);
                registry.Register(TableMonsterDropRate);
                registry.Register(TableNpcDropRate);
                registry.Register(TableItemDropGroup);
                registry.Register(TableExp);
                registry.Register(TableWindow);
                // Legacy는 로드하지 않는다(프로젝트가 정말로 필요하면 외부에서 RegistryTable로 등록)
                // registry.Register(TableStatus);

                registry.Register(TableStat);
                registry.Register(TableDamageType);
                registry.Register(TableState);
                registry.Register(TableSkill);
                registry.Register(TableAffect);
                registry.Register(TableAffectModifier);
                registry.Register(TableEffect);
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

        // Skill
        public StruckTableSkill GetSkillData(int uid, bool logIfMissing = true)
            => GetData(TableSkill, uid, "Skill", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetSkillData(int uid, out StruckTableSkill data, bool logIfMissing = false)
            => TryGetData(TableSkill, uid, out data, "Skill", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Affect
        public StruckTableAffect GetAffectData(int uid, bool logIfMissing = true)
            => GetData(TableAffect, uid, "Affect", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetAffectData(int uid, out StruckTableAffect data, bool logIfMissing = false)
            => TryGetData(TableAffect, uid, out data, "Affect", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Effect
        public StruckTableEffect GetEffectData(int uid, bool logIfMissing = true)
            => GetData(TableEffect, uid, "Effect", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetEffectData(int uid, out StruckTableEffect data, bool logIfMissing = false)
            => TryGetData(TableEffect, uid, out data, "Effect", (t, i) => t.GetDataByUid(i), logIfMissing);

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
