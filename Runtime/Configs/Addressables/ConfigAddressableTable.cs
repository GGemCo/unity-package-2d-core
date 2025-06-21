using System.Collections.Generic;

namespace GGemCo.Scripts
{
    public static class ConfigAddressableTable
    {
        public const string Map = "map";
        public const string Monster = "monster";
        public const string Npc = "npc";
        public const string Animation = "animation";
        public const string Item = "item";
        public const string MonsterDropRate = "monster_drop_rate";
        public const string ItemDropGroup = "item_drop_group";
        public const string Exp = "exp";
        public const string Window = "window";
        public const string Status = "status";
        public const string Skill = "skill";
        public const string Affect = "affect";
        public const string Effect = "effect";
        public const string Interaction = "interaction";
        public const string Shop = "shop";
        public const string ItemUpgrade = "item_upgrade";
        public const string ItemSalvage = "item_salvage";
        public const string ItemCraft = "item_craft";
        public const string Cutscene = "cutscene";
        public const string Dialogue = "dialogue";
        public const string Quest = "quest";
        
        private static string TablePath() => $"{ConfigAddressables.Path}/Tables";

        public static readonly AddressableAssetInfo TableMap = new(
            $"{ConfigAddressableLabel.Table}_{Map}",
            $"{TablePath()}/{Map}.txt",
            ConfigAddressableLabel.Table,
            Map
        );

        public static readonly AddressableAssetInfo TableMonster = new(
            $"{ConfigAddressableLabel.Table}_{Monster}",
            $"{TablePath()}/{Monster}.txt",
            ConfigAddressableLabel.Table,
            Monster
        );
        public static readonly AddressableAssetInfo TableNpc = new(
            $"{ConfigAddressableLabel.Table}_{Npc}",
            $"{TablePath()}/{Npc}.txt",
            ConfigAddressableLabel.Table,
            Npc
        );
        public static readonly AddressableAssetInfo TableAnimation = new(
            $"{ConfigAddressableLabel.Table}_{Animation}",
            $"{TablePath()}/{Animation}.txt",
            ConfigAddressableLabel.Table,
            Animation
        );
        public static readonly AddressableAssetInfo TableItem = new(
            $"{ConfigAddressableLabel.Table}_{Item}",
            $"{TablePath()}/{Item}.txt",
            ConfigAddressableLabel.Table,
            Item
        );
        public static readonly AddressableAssetInfo TableMonsterDropRate = new(
            $"{ConfigAddressableLabel.Table}_{MonsterDropRate}",
            $"{TablePath()}/{MonsterDropRate}.txt",
            ConfigAddressableLabel.Table,
            MonsterDropRate
        );
        public static readonly AddressableAssetInfo TableItemDropGroup = new(
            $"{ConfigAddressableLabel.Table}_{ItemDropGroup}",
            $"{TablePath()}/{ItemDropGroup}.txt",
            ConfigAddressableLabel.Table,
            ItemDropGroup
        );
        public static readonly AddressableAssetInfo TableExp = new(
            $"{ConfigAddressableLabel.Table}_{Exp}",
            $"{TablePath()}/{Exp}.txt",
            ConfigAddressableLabel.Table,
            Exp
        );
        public static readonly AddressableAssetInfo TableWindow = new(
            $"{ConfigAddressableLabel.Table}_{Window}",
            $"{TablePath()}/{Window}.txt",
            ConfigAddressableLabel.Table,
            Window
        );
        public static readonly AddressableAssetInfo TableStatus = new(
            $"{ConfigAddressableLabel.Table}_{Status}",
            $"{TablePath()}/{Status}.txt",
            ConfigAddressableLabel.Table,
            Status
        );
        public static readonly AddressableAssetInfo TableSkill = new(
            $"{ConfigAddressableLabel.Table}_{Skill}",
            $"{TablePath()}/{Skill}.txt",
            ConfigAddressableLabel.Table,
            Skill
        );
        public static readonly AddressableAssetInfo TableAffect = new(
            $"{ConfigAddressableLabel.Table}_{Affect}",
            $"{TablePath()}/{Affect}.txt",
            ConfigAddressableLabel.Table,
            Affect
        );
        public static readonly AddressableAssetInfo TableEffect = new(
            $"{ConfigAddressableLabel.Table}_{Effect}",
            $"{TablePath()}/{Effect}.txt",
            ConfigAddressableLabel.Table,
            Effect
        );
        public static readonly AddressableAssetInfo TableInteraction = new(
            $"{ConfigAddressableLabel.Table}_{Interaction}",
            $"{TablePath()}/{Interaction}.txt",
            ConfigAddressableLabel.Table,
            Interaction
        );
        public static readonly AddressableAssetInfo TableShop = new(
            $"{ConfigAddressableLabel.Table}_{Shop}",
            $"{TablePath()}/{Shop}.txt",
            ConfigAddressableLabel.Table,
            Shop
        );
        public static readonly AddressableAssetInfo TableItemUpgrade = new(
            $"{ConfigAddressableLabel.Table}_{ItemUpgrade}",
            $"{TablePath()}/{ItemUpgrade}.txt",
            ConfigAddressableLabel.Table,
            ItemUpgrade
        );
        public static readonly AddressableAssetInfo TableItemSalvage = new(
            $"{ConfigAddressableLabel.Table}_{ItemSalvage}",
            $"{TablePath()}/{ItemSalvage}.txt",
            ConfigAddressableLabel.Table,
            ItemSalvage
        );
        public static readonly AddressableAssetInfo TableItemCraft = new(
            $"{ConfigAddressableLabel.Table}_{ItemCraft}",
            $"{TablePath()}/{ItemCraft}.txt",
            ConfigAddressableLabel.Table,
            ItemCraft
        );
        public static readonly AddressableAssetInfo TableCutscene = new(
            $"{ConfigAddressableLabel.Table}_{Cutscene}",
            $"{TablePath()}/{Cutscene}.txt",
            ConfigAddressableLabel.Table,
            Cutscene
        );
        public static readonly AddressableAssetInfo TableDialogue = new(
            $"{ConfigAddressableLabel.Table}_{Dialogue}",
            $"{TablePath()}/{Dialogue}.txt",
            ConfigAddressableLabel.Table,
            Dialogue
        );
        public static readonly AddressableAssetInfo TableQuest = new(
            $"{ConfigAddressableLabel.Table}_{Quest}",
            $"{TablePath()}/{Quest}.txt",
            ConfigAddressableLabel.Table,
            Quest
        );

        public static readonly List<AddressableAssetInfo> All = new()
        {
            TableMap,
            TableMonster,
            TableNpc,
            TableAnimation,
            TableItem,
            TableMonsterDropRate,
            TableItemDropGroup,
            TableExp,
            TableWindow,
            TableStatus,
            TableAffect,
            TableEffect,
            TableInteraction,
            TableShop,
            TableItemUpgrade,
            TableItemSalvage,
            TableItemCraft,
            TableCutscene,
            TableDialogue,
            TableQuest,
        };
    }
}