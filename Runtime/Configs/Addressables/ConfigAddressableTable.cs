using System.Collections.Generic;

namespace GGemCo2DCore
{
    public static class ConfigAddressableTable
    {
        // 파일 확장자(.txt → .csv/.tsv/.json 전환 용이)
        private const string FileExt = ".txt";
        private static string TablePath => ConfigAddressablePath.Tables;

        public static AddressableAssetInfo Make(string tableName)
        {
            var key  = $"{ConfigAddressableKey.Table}_{tableName}";
            var path = $"{TablePath}/{tableName}{FileExt}";
            return new AddressableAssetInfo(key, path, ConfigAddressableLabel.Table, tableName);
        }

        // 테이블 이름들 (필요 시 enum으로 승격 가능)
        public const string None             = "None";
        public const string Map              = "map";
        public const string Monster          = "monster";
        public const string Npc              = "npc";
        public const string Animation        = "animation";
        public const string Item             = "item";
        public const string ItemBaseOption   = "item_base_option";
        public const string ItemAffixDef     = "item_affix_def";
        public const string ItemAffixPool    = "item_affix_pool";
        public const string ItemRollRule     = "item_roll_rule";
        public const string MonsterDropRate  = "monster_drop_rate";
        public const string NpcDropRate      = "npc_drop_rate";
        public const string ItemDropGroup    = "item_drop_group";
        public const string Exp              = "exp";
        public const string Window           = "window";
        public const string Stat             = "stat";
        public const string DamageType       = "damage_type";
        public const string State            = "state";
        public const string VfxEffect        = "vfx_effect";
        public const string VfxParticle      = "vfx_particle";
        public const string Interaction      = "interaction";
        public const string Shop             = "shop";
        public const string ItemUpgrade      = "item_upgrade";
        public const string ItemSalvage      = "item_salvage";
        public const string ItemCraft        = "item_craft";
        public const string Cutscene         = "cutscene";
        public const string Dialogue         = "dialogue";
        public const string Quest            = "quest";
        public const string Projectile       = "projectile";
        public const string Sound            = "sound";
        public const string SimulationTool   = "simulation_tool";
        public const string SimulationGrowth = "simulation_growth";
        public const string CrowdControl          = "crowd_control";
        public const string CrowdControlKnockBack = "crowd_control_knock_back";
        public const string CrowdControlKnockDown = "crowd_control_knock_down";
        public const string CrowdControlKnockUp   = "crowd_control_knock_up";
        public const string ItemUse          = "item_use";
        public const string ItemUseAction    = "item_use_action";

        // 개별 항목
        public static readonly AddressableAssetInfo TableMap             = Make(Map);
        public static readonly AddressableAssetInfo TableMonster         = Make(Monster);
        public static readonly AddressableAssetInfo TableNpc             = Make(Npc);
        public static readonly AddressableAssetInfo TableAnimation       = Make(Animation);
        public static readonly AddressableAssetInfo TableItem            = Make(Item);
        public static readonly AddressableAssetInfo TableItemBaseOption  = Make(ItemBaseOption);
        public static readonly AddressableAssetInfo TableItemAffixDef    = Make(ItemAffixDef);
        public static readonly AddressableAssetInfo TableItemAffixPool   = Make(ItemAffixPool);
        public static readonly AddressableAssetInfo TableItemRollRule    = Make(ItemRollRule);
        public static readonly AddressableAssetInfo TableMonsterDropRate = Make(MonsterDropRate);
        public static readonly AddressableAssetInfo TableNpcDropRate     = Make(NpcDropRate);
        public static readonly AddressableAssetInfo TableItemDropGroup   = Make(ItemDropGroup);
        public static readonly AddressableAssetInfo TableExp             = Make(Exp);
        public static readonly AddressableAssetInfo TableWindow          = Make(Window);
        public static readonly AddressableAssetInfo TableStat            = Make(Stat);
        public static readonly AddressableAssetInfo TableDamageType      = Make(DamageType);
        public static readonly AddressableAssetInfo TableState           = Make(State);
        public static readonly AddressableAssetInfo TableVfxEffect       = Make(VfxEffect);
        public static readonly AddressableAssetInfo TableVfxParticle     = Make(VfxParticle);
        public static readonly AddressableAssetInfo TableInteraction     = Make(Interaction);
        public static readonly AddressableAssetInfo TableShop            = Make(Shop);
        public static readonly AddressableAssetInfo TableItemUpgrade     = Make(ItemUpgrade);
        public static readonly AddressableAssetInfo TableItemSalvage     = Make(ItemSalvage);
        public static readonly AddressableAssetInfo TableItemCraft       = Make(ItemCraft);
        public static readonly AddressableAssetInfo TableCutscene        = Make(Cutscene);
        public static readonly AddressableAssetInfo TableDialogue        = Make(Dialogue);
        public static readonly AddressableAssetInfo TableQuest           = Make(Quest);
        public static readonly AddressableAssetInfo TableProjectile      = Make(Projectile);
        public static readonly AddressableAssetInfo TableSound           = Make(Sound);
        public static readonly AddressableAssetInfo TableSimulationTool  = Make(SimulationTool);
        public static readonly AddressableAssetInfo TableSimulationGrowth  = Make(SimulationGrowth);
        public static readonly AddressableAssetInfo TableCrowdControl          = Make(CrowdControl);
        public static readonly AddressableAssetInfo TableCrowdControlKnockBack = Make(CrowdControlKnockBack);
        public static readonly AddressableAssetInfo TableCrowdControlKnockDown = Make(CrowdControlKnockDown);
        public static readonly AddressableAssetInfo TableCrowdControlKnockUp   = Make(CrowdControlKnockUp);
        public static readonly AddressableAssetInfo TableItemUse         = Make(ItemUse);
        public static readonly AddressableAssetInfo TableItemUseAction   = Make(ItemUseAction);

        // 전체 목록 + 읽기 전용 뷰
        public static readonly List<AddressableAssetInfo> All = new()
        {
            TableMap, TableMonster, TableNpc, TableAnimation, TableItem,
            TableItemBaseOption, TableItemAffixDef, TableItemAffixPool, TableItemRollRule,
            TableMonsterDropRate, TableNpcDropRate, TableItemDropGroup, TableExp, TableWindow,
            // Status 3분리 테이블
            TableStat, TableDamageType, TableState,
            // Others
            TableVfxEffect, TableVfxParticle, TableInteraction,
            TableShop, TableItemUpgrade, TableItemSalvage, TableItemCraft,
            TableCutscene, TableDialogue, TableQuest, TableProjectile, TableSound, TableSimulationTool,
            TableSimulationGrowth, TableCrowdControl, TableCrowdControlKnockBack, TableCrowdControlKnockDown, TableCrowdControlKnockUp, TableItemUse, TableItemUseAction
        };
        public static AddressableAssetInfo GetByKey(string key)
        {
            return All.Find(assetInfo => assetInfo.Key == key);
        }

        // 편의 API
        public static string KeySoundTable() => $"{ConfigAddressableLabel.Table}_{Sound}";
    }
}