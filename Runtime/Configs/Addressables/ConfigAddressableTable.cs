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
        public const string MapEntryRule     = "map_entry_rule";
        public const string Monster          = "monster";
        public const string MonsterPhase     = "monster_phase";
        public const string Npc              = "npc";
        public const string Animation        = "animation";
        public const string Item             = "item";
        public const string ItemVisual       = "item_visual";
        public const string ItemBaseOption   = "item_base_option";
        public const string ItemAffixDef     = "item_affix_def";
        public const string ItemAffixPool    = "item_affix_pool";
        public const string ItemRollRule     = "item_roll_rule";
        public const string MonsterDropRate  = "monster_drop_rate";
        public const string NpcDropRate      = "npc_drop_rate";
        public const string ItemDropGroup    = "item_drop_group";
        public const string Exp              = "exp";
        public const string Window           = "window";
        public const string UIEffect         = "ui_effect";
        public const string Stat             = "stat";
        public const string DamageType       = "damage_type";
        public const string State            = "state";
        public const string Vfx              = "vfx";
        public const string VfxEffect        = "vfx_effect";
        public const string VfxParticle      = "vfx_particle";
        public const string VfxVariant       = "vfx_variant";
        public const string Interaction      = "interaction";
        public const string Shop             = "shop";
        public const string ShopItem         = "shop_item";
        public const string ShopPromotion    = "shop_promotion";
        public const string ItemUpgrade      = "item_upgrade";
        public const string ItemSalvage      = "item_salvage";
        public const string ItemCraft        = "item_craft";
        public const string Cutscene         = "cutscene";
        public const string Dialogue         = "dialogue";
        public const string Quest            = "quest";
        public const string License          = "license";
        public const string Projectile       = "projectile";
        public const string Laser            = "laser";
        public const string ProjectileLinear = "projectile_linear";
        public const string ProjectileArc    = "projectile_arc";
        public const string ProjectilePath   = "projectile_path";
        public const string ProjectileLinearThenSegments = "projectile_linear_then_segments";
        public const string Sound            = "sound";
        public const string SoundBgm         = "sound_bgm";
        public const string SoundAmbient     = "sound_ambient";
        public const string SoundSfx         = "sound_sfx";
        public const string SoundVariant     = "sound_variant";
        public const string SimulationTool   = "simulation_tool";
        public const string SimulationGrowth = "simulation_growth";
        public const string CrowdControl          = "crowd_control";
        public const string CrowdControlKnockBack = "crowd_control_knock_back";
        public const string CrowdControlKnockDown = "crowd_control_knock_down";
        public const string CrowdControlKnockUp   = "crowd_control_knock_up";
        public const string CrowdControlKnockDownAir = "crowd_control_knock_down_air";
        public const string ItemUse          = "item_use";
        public const string ItemUseAction    = "item_use_action";

        // 개별 항목
        public static readonly AddressableAssetInfo TableMap             = Make(Map);
        public static readonly AddressableAssetInfo TableMapEntryRule    = Make(MapEntryRule);
        public static readonly AddressableAssetInfo TableMonster         = Make(Monster);
        public static readonly AddressableAssetInfo TableMonsterPhase    = Make(MonsterPhase);
        public static readonly AddressableAssetInfo TableNpc             = Make(Npc);
        public static readonly AddressableAssetInfo TableAnimation       = Make(Animation);
        public static readonly AddressableAssetInfo TableItem            = Make(Item);
        public static readonly AddressableAssetInfo TableItemVisual      = Make(ItemVisual);
        public static readonly AddressableAssetInfo TableItemBaseOption  = Make(ItemBaseOption);
        public static readonly AddressableAssetInfo TableItemAffixDef    = Make(ItemAffixDef);
        public static readonly AddressableAssetInfo TableItemAffixPool   = Make(ItemAffixPool);
        public static readonly AddressableAssetInfo TableItemRollRule    = Make(ItemRollRule);
        public static readonly AddressableAssetInfo TableMonsterDropRate = Make(MonsterDropRate);
        public static readonly AddressableAssetInfo TableNpcDropRate     = Make(NpcDropRate);
        public static readonly AddressableAssetInfo TableItemDropGroup   = Make(ItemDropGroup);
        public static readonly AddressableAssetInfo TableExp             = Make(Exp);
        public static readonly AddressableAssetInfo TableWindow          = Make(Window);
        public static readonly AddressableAssetInfo TableUIEffect        = Make(UIEffect);
        public static readonly AddressableAssetInfo TableStat            = Make(Stat);
        public static readonly AddressableAssetInfo TableDamageType      = Make(DamageType);
        public static readonly AddressableAssetInfo TableState           = Make(State);
        public static readonly AddressableAssetInfo TableVfx             = Make(Vfx);
        public static readonly AddressableAssetInfo TableVfxEffect       = Make(VfxEffect);
        public static readonly AddressableAssetInfo TableVfxParticle     = Make(VfxParticle);
        public static readonly AddressableAssetInfo TableVfxVariant      = Make(VfxVariant);
        public static readonly AddressableAssetInfo TableInteraction     = Make(Interaction);
        public static readonly AddressableAssetInfo TableShop            = Make(Shop);
        public static readonly AddressableAssetInfo TableShopItem        = Make(ShopItem);
        public static readonly AddressableAssetInfo TableShopPromotion   = Make(ShopPromotion);
        public static readonly AddressableAssetInfo TableItemUpgrade     = Make(ItemUpgrade);
        public static readonly AddressableAssetInfo TableItemSalvage     = Make(ItemSalvage);
        public static readonly AddressableAssetInfo TableItemCraft       = Make(ItemCraft);
        public static readonly AddressableAssetInfo TableCutscene        = Make(Cutscene);
        public static readonly AddressableAssetInfo TableDialogue        = Make(Dialogue);
        public static readonly AddressableAssetInfo TableQuest           = Make(Quest);
        public static readonly AddressableAssetInfo TableLicense         = Make(License);
        public static readonly AddressableAssetInfo TableProjectile      = Make(Projectile);
        public static readonly AddressableAssetInfo TableLaser           = Make(Laser);
        public static readonly AddressableAssetInfo TableProjectileLinear = Make(ProjectileLinear);
        public static readonly AddressableAssetInfo TableProjectileArc    = Make(ProjectileArc);
        public static readonly AddressableAssetInfo TableProjectilePath   = Make(ProjectilePath);
        public static readonly AddressableAssetInfo TableProjectileLinearThenSegments = Make(ProjectileLinearThenSegments);
        public static readonly AddressableAssetInfo TableSound           = Make(Sound);
        public static readonly AddressableAssetInfo TableSoundBgm        = Make(SoundBgm);
        public static readonly AddressableAssetInfo TableSoundAmbient    = Make(SoundAmbient);
        public static readonly AddressableAssetInfo TableSoundSfx        = Make(SoundSfx);
        public static readonly AddressableAssetInfo TableSoundVariant    = Make(SoundVariant);
        public static readonly AddressableAssetInfo TableSimulationTool  = Make(SimulationTool);
        public static readonly AddressableAssetInfo TableSimulationGrowth  = Make(SimulationGrowth);
        public static readonly AddressableAssetInfo TableCrowdControl          = Make(CrowdControl);
        public static readonly AddressableAssetInfo TableCrowdControlKnockBack = Make(CrowdControlKnockBack);
        public static readonly AddressableAssetInfo TableCrowdControlKnockDown = Make(CrowdControlKnockDown);
        public static readonly AddressableAssetInfo TableCrowdControlKnockUp   = Make(CrowdControlKnockUp);
        public static readonly AddressableAssetInfo TableCrowdControlKnockDownAir = Make(CrowdControlKnockDownAir);
        public static readonly AddressableAssetInfo TableItemUse         = Make(ItemUse);
        public static readonly AddressableAssetInfo TableItemUseAction   = Make(ItemUseAction);

        // 전체 목록 + 읽기 전용 뷰
        public static readonly List<AddressableAssetInfo> All = new()
        {
            TableMap, TableMapEntryRule, TableMonster, TableMonsterPhase, TableNpc, TableAnimation, TableItem, TableItemVisual,
            TableItemBaseOption, TableItemAffixDef, TableItemAffixPool, TableItemRollRule,
            TableMonsterDropRate, TableNpcDropRate, TableItemDropGroup, TableExp, TableWindow, TableUIEffect,
            // Status 3분리 테이블
            TableStat, TableDamageType, TableState,
            // Others
            TableVfx, TableVfxEffect, TableVfxParticle, TableVfxVariant, TableInteraction,
            TableShop, TableShopItem, TableShopPromotion, TableItemUpgrade, TableItemSalvage, TableItemCraft,
            TableCutscene, TableDialogue, TableQuest, TableLicense,
            TableProjectile, TableLaser, TableProjectileLinear, TableProjectileArc, TableProjectilePath, TableProjectileLinearThenSegments,
            TableSound, TableSoundBgm, TableSoundAmbient, TableSoundSfx, TableSoundVariant, TableSimulationTool,
            TableSimulationGrowth, TableCrowdControl, TableCrowdControlKnockBack, TableCrowdControlKnockDown, TableCrowdControlKnockUp, TableCrowdControlKnockDownAir, TableItemUse, TableItemUseAction
        };
        public static AddressableAssetInfo GetByKey(string key)
        {
            return All.Find(assetInfo => assetInfo.Key == key);
        }

        // 편의 API
        public static string KeySoundTable() => $"{ConfigAddressableLabel.Table}_{Sound}";
    }
}
