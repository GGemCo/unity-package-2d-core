using System.Collections.Generic;
using UnityEngine;

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
        public TableMapEntryRule TableMapEntryRule { get; private set; } = new TableMapEntryRule();
        public TableMonster TableMonster { get; private set; } = new TableMonster();
        public TableAnimation TableAnimation { get; private set; } = new TableAnimation();
        public TableItem TableItem { get; private set; } = new TableItem();
        public TableItemVisual TableItemVisual { get; private set; } = new TableItemVisual();
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
        public TableCrowdControlKnockBack TableCrowdControlKnockBack { get; private set; } = new TableCrowdControlKnockBack();
        public TableCrowdControlKnockDown TableCrowdControlKnockDown { get; private set; } = new TableCrowdControlKnockDown();
        public TableCrowdControlKnockUp TableCrowdControlKnockUp { get; private set; } = new TableCrowdControlKnockUp();
        public TableCrowdControlKnockDownAir TableCrowdControlKnockDownAir { get; private set; } = new TableCrowdControlKnockDownAir();
        public TableVfxEffect TableVfxEffect { get; private set; } = new TableVfxEffect();
        public TableVfxParticle TableVfxParticle { get; private set; } = new TableVfxParticle();
        public TableInteraction TableInteraction { get; private set; } = new TableInteraction();
        public TableShop TableShop { get; private set; } = new TableShop();
        public TableShopItem TableShopItem { get; private set; } = new TableShopItem();
        public TableShopPromotion TableShopPromotion { get; private set; } = new TableShopPromotion();
        public TableItemUpgrade TableItemUpgrade { get; private set; } = new TableItemUpgrade();
        public TableItemSalvage TableItemSalvage { get; private set; } = new TableItemSalvage();
        public TableItemCraft TableItemCraft { get; private set; } = new TableItemCraft();
        public TableCutscene TableCutscene { get; private set; } = new TableCutscene();
        public TableDialogue TableDialogue { get; private set; } = new TableDialogue();
        public TableQuest TableQuest { get; private set; } = new TableQuest();
        public TableLicense TableLicense { get; private set; } = new TableLicense();
        public TableProjectile TableProjectile { get; private set; } = new TableProjectile();
        public TableLaser TableLaser { get; private set; } = new TableLaser();
        public TableProjectileLinear TableProjectileLinear { get; private set; } = new TableProjectileLinear();
        public TableProjectileArc TableProjectileArc { get; private set; } = new TableProjectileArc();
        public TableProjectilePath TableProjectilePath { get; private set; } = new TableProjectilePath();
        public TableProjectileLinearThenSegments TableProjectileLinearThenSegments { get; private set; } = new TableProjectileLinearThenSegments();
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
                registry.Register(TableMapEntryRule);
                registry.Register(TableItem);
                registry.Register(TableItemVisual);
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
                registry.Register(TableCrowdControlKnockBack);
                registry.Register(TableCrowdControlKnockDown);
                registry.Register(TableCrowdControlKnockUp);
                registry.Register(TableCrowdControlKnockDownAir);
                registry.Register(TableVfxEffect);
                registry.Register(TableVfxParticle);
                registry.Register(TableInteraction);
                registry.Register(TableShop);
                registry.Register(TableShopItem);
                registry.Register(TableShopPromotion);
                registry.Register(TableItemUpgrade);
                registry.Register(TableItemSalvage);
                registry.Register(TableItemCraft);
                registry.Register(TableCutscene);
                registry.Register(TableDialogue);
                registry.Register(TableQuest);
                registry.Register(TableLicense);
                registry.Register(TableProjectile);
                registry.Register(TableLaser);
                registry.Register(TableProjectileLinear);
                registry.Register(TableProjectileArc);
                registry.Register(TableProjectilePath);
                registry.Register(TableProjectileLinearThenSegments);
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

        /// <summary>
        /// map_entry_rule 테이블에서 UID로 맵 입장 규칙을 조회합니다.
        /// </summary>
        /// <param name="uid">조회할 맵 입장 규칙 UID입니다.</param>
        /// <param name="logIfMissing">조회 실패 시 경고 로그를 남길지 여부입니다.</param>
        /// <returns>찾은 맵 입장 규칙입니다. 없으면 null을 반환합니다.</returns>
        public StruckTableMapEntryRule GetMapEntryRuleData(int uid, bool logIfMissing = true)
            => GetData(TableMapEntryRule, uid, "MapEntryRule", (t, i) => t.GetDataByUid(i), logIfMissing);

        /// <summary>
        /// map_entry_rule 테이블에서 UID로 맵 입장 규칙 조회를 시도합니다.
        /// </summary>
        /// <param name="uid">조회할 맵 입장 규칙 UID입니다.</param>
        /// <param name="data">조회에 성공하면 맵 입장 규칙이 설정됩니다.</param>
        /// <param name="logIfMissing">조회 실패 시 경고 로그를 남길지 여부입니다.</param>
        /// <returns>맵 입장 규칙을 찾으면 true를 반환합니다.</returns>
        public bool TryGetMapEntryRuleData(int uid, out StruckTableMapEntryRule data, bool logIfMissing = false)
            => TryGetData(TableMapEntryRule, uid, out data, "MapEntryRule", (t, i) => t.GetDataByUid(i), logIfMissing);

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

        // CrowdControl
        public StruckTableCrowdControl GetCrowdControlData(int uid, bool logIfMissing = true)
            => GetData(TableCrowdControl, uid, "CrowdControl", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetCrowdControlData(int uid, out StruckTableCrowdControl data, bool logIfMissing = false)
            => TryGetData(TableCrowdControl, uid, out data, "CrowdControl", (t, i) => t.GetDataByUid(i), logIfMissing);

        public CrowdControlRuntimeData GetCrowdControlRuntimeData(int uid, bool logIfMissing = true)
        {
            var data = GetCrowdControlData(uid, logIfMissing);
            return CrowdControlRuntimeDataResolver.Resolve(this, data);
        }

        public bool TryGetCrowdControlRuntimeData(int uid, out CrowdControlRuntimeData data, bool logIfMissing = false)
        {
            data = GetCrowdControlRuntimeData(uid, logIfMissing);
            return data != null;
        }

        // Vfx
        public VfxRuntimeData GetVfxData(int uid, bool logIfMissing = true)
        {
            if (uid <= 0)
                return null;

            if (TableVfxEffect != null && TableVfxEffect.TryGetDataByUid(uid, out var effectRow))
                return VfxRuntimeDataFactory.Create(effectRow);

            if (TableVfxParticle != null && TableVfxParticle.TryGetDataByUid(uid, out var particleRow))
                return VfxRuntimeDataFactory.Create(particleRow);

            if (logIfMissing)
                GcLogger.LogError($"Vfx 데이터를 찾을 수 없습니다. uid={uid}");

            return null;
        }

        public bool TryGetVfxData(int uid, out VfxRuntimeData data, bool logIfMissing = false)
        {
            data = GetVfxData(uid, logIfMissing);
            return data != null;
        }

        public IReadOnlyDictionary<int, VfxRuntimeData> GetAllVfxData()
        {
            var merged = new Dictionary<int, VfxRuntimeData>();

            MergeVfxRows(merged, TableVfxEffect?.GetAll());
            MergeVfxRows(merged, TableVfxParticle?.GetAll());
            return merged;
        }

        private static void MergeVfxRows(Dictionary<int, VfxRuntimeData> target, IReadOnlyDictionary<int, StruckTableVfxEffect> source)
        {
            if (target == null || source == null)
                return;

            foreach (var pair in source)
                target[pair.Key] = VfxRuntimeDataFactory.Create(pair.Value);
        }

        private static void MergeVfxRows(Dictionary<int, VfxRuntimeData> target, IReadOnlyDictionary<int, StruckTableVfxParticle> source)
        {
            if (target == null || source == null)
                return;

            foreach (var pair in source)
                target[pair.Key] = VfxRuntimeDataFactory.Create(pair.Value);
        }

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

        /// <summary>
        /// license 테이블에서 UID로 라이센스 정의를 조회합니다.
        /// </summary>
        /// <param name="uid">조회할 라이센스 UID입니다.</param>
        /// <param name="logIfMissing">조회 실패 시 경고 로그를 남길지 여부입니다.</param>
        /// <returns>찾은 라이센스 정의입니다. 없으면 null을 반환합니다.</returns>
        public StruckTableLicense GetLicenseData(int uid, bool logIfMissing = true)
            => GetData(TableLicense, uid, "License", (t, i) => t.GetDataByUid(i), logIfMissing);

        /// <summary>
        /// license 테이블에서 UID로 라이센스 정의 조회를 시도합니다.
        /// </summary>
        /// <param name="uid">조회할 라이센스 UID입니다.</param>
        /// <param name="data">조회에 성공하면 라이센스 정의가 설정됩니다.</param>
        /// <param name="logIfMissing">조회 실패 시 경고 로그를 남길지 여부입니다.</param>
        /// <returns>라이센스 정의를 찾으면 true를 반환합니다.</returns>
        public bool TryGetLicenseData(int uid, out StruckTableLicense data, bool logIfMissing = false)
            => TryGetData(TableLicense, uid, out data, "License", (t, i) => t.GetDataByUid(i), logIfMissing);

        /// <summary>
        /// license 테이블에서 Key로 라이센스 정의를 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="logIfMissing">조회 실패 시 경고 로그를 남길지 여부입니다.</param>
        /// <returns>찾은 라이센스 정의입니다. 없으면 null을 반환합니다.</returns>
        public StruckTableLicense GetLicenseDataByKey(string key, bool logIfMissing = true)
        {
            if (TableLicense == null)
            {
                if (logIfMissing)
                    GcLogger.LogWarning("[Table] License table is null.");
                return null;
            }

            StruckTableLicense data = TableLicense.GetDataByKey(key);
            if (data == null && logIfMissing)
                GcLogger.LogWarning($"[Table] License not found. key={key}");
            return data;
        }

        /// <summary>
        /// license 테이블에서 Key로 라이센스 정의 조회를 시도합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="data">조회에 성공하면 라이센스 정의가 설정됩니다.</param>
        /// <param name="logIfMissing">조회 실패 시 경고 로그를 남길지 여부입니다.</param>
        /// <returns>라이센스 정의를 찾으면 true를 반환합니다.</returns>
        public bool TryGetLicenseDataByKey(string key, out StruckTableLicense data, bool logIfMissing = false)
        {
            data = GetLicenseDataByKey(key, logIfMissing);
            return data != null;
        }

        /// <summary>
        /// laser.txt 정적 데이터를 조회합니다.
        /// </summary>
        /// <param name="uid">조회할 레이저 UID입니다.</param>
        /// <param name="logIfMissing">조회 실패 시 경고 로그를 남길지 여부입니다.</param>
        /// <returns>찾은 레이저 데이터입니다. 없으면 null을 반환합니다.</returns>
        public StruckTableLaser GetLaserData(int uid, bool logIfMissing = true)
            => GetData(TableLaser, uid, "Laser", (t, i) => t.GetDataByUid(i), logIfMissing);

        /// <summary>
        /// laser.txt 정적 데이터 조회를 시도합니다.
        /// </summary>
        /// <param name="uid">조회할 레이저 UID입니다.</param>
        /// <param name="data">조회에 성공하면 레이저 데이터가 설정됩니다.</param>
        /// <param name="logIfMissing">조회 실패 시 경고 로그를 남길지 여부입니다.</param>
        /// <returns>레이저 데이터를 찾으면 true를 반환합니다.</returns>
        public bool TryGetLaserData(int uid, out StruckTableLaser data, bool logIfMissing = false)
            => TryGetData(TableLaser, uid, out data, "Laser", (t, i) => t.GetDataByUid(i), logIfMissing);

        /// <summary>
        /// projectile.txt 공용 Row와 projectile_linear/arc/path 상세 Row를 병합해 발사체 정의를 조회합니다.
        /// - 공용 Row가 없으면 상세 Row만으로는 발사체를 생성하지 않습니다.
        /// - 상세 Row가 없으면 공용 Row에 남아 있는 기본값 또는 레거시 상세 컬럼 값을 사용합니다.
        /// </summary>
        /// <param name="uid">조회할 Projectile UID입니다.</param>
        /// <param name="logIfMissing">조회 실패 시 경고 로그를 남길지 여부입니다.</param>
        /// <returns>Projectile Row를 찾으면 반환하고, 없으면 null을 반환합니다.</returns>
        public StruckTableProjectile GetProjectileData(int uid, bool logIfMissing = true)
        {
            if (TryGetProjectileData(uid, out StruckTableProjectile data, false))
                return data;

            if (logIfMissing)
                GcLogger.LogWarning($"[Table] Projectile not found. uid={uid}");

            return null;
        }

        /// <summary>
        /// projectile.txt 공용 Row와 타입별 상세 Row를 합쳐 발사체 정의 조회를 시도합니다.
        /// </summary>
        /// <param name="uid">조회할 Projectile UID입니다.</param>
        /// <param name="data">조회에 성공하면 Projectile Row가 설정됩니다.</param>
        /// <param name="logIfMissing">조회 실패 시 경고 로그를 남길지 여부입니다.</param>
        /// <returns>Projectile Row를 찾으면 true를 반환합니다.</returns>
        public bool TryGetProjectileData(int uid, out StruckTableProjectile data, bool logIfMissing = false)
        {
            data = null;
            if (TableProjectile != null && TableProjectile.TryGetDataByUid(uid, out StruckTableProjectile common))
            {
                StruckTableProjectileLinear linear = null;
                StruckTableProjectileArc arc = null;
                StruckTableProjectilePath path = null;
                StruckTableProjectileLinearThenSegments linearThenSegments = null;

                if (TableProjectileLinear != null)
                    TableProjectileLinear.TryGetDataByUid(uid, out linear);

                if (TableProjectileArc != null)
                    TableProjectileArc.TryGetDataByUid(uid, out arc);

                if (TableProjectilePath != null)
                    TableProjectilePath.TryGetDataByUid(uid, out path);

                if (TableProjectileLinearThenSegments != null)
                    TableProjectileLinearThenSegments.TryGetDataByUid(uid, out linearThenSegments);

                data = TableProjectile.CreateMerged(common, linear, arc, path, linearThenSegments);
                return true;
            }

            if (logIfMissing)
                GcLogger.LogWarning($"[Table] Projectile not found. uid={uid}");

            return false;
        }

        // Sound
        public StruckTableSound GetSoundData(int uid, bool logIfMissing = true)
            => GetData(TableSound, uid, "Sound", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetSoundData(int uid, out StruckTableSound data, bool logIfMissing = false)
            => TryGetData(TableSound, uid, out data, "Sound", (t, i) => t.GetDataByUid(i), logIfMissing);

    }
}
