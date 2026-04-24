namespace GGemCo2DCore
{
    public static class ItemLocalizationKeys
    {
        public static string Type(ItemConstants.Type value) => value switch
        {
            ItemConstants.Type.Equip => "Item_Type_Equip",
            ItemConstants.Type.Consumable => "Item_Type_Consumable",
            ItemConstants.Type.Currency => "Item_Type_Currency",
            ItemConstants.Type.Misc => "Item_Type_Misc",
            _ => string.Empty
        };

        public static string Category(ItemConstants.Category value) => value switch
        {
            ItemConstants.Category.Weapon => "Item_Category_Weapon",
            ItemConstants.Category.Armor => "Item_Category_Armor",
            ItemConstants.Category.Potion => "Item_Category_Potion",
            ItemConstants.Category.Gold => "Item_Category_Gold",
            ItemConstants.Category.Silver => "Item_Category_Silver",
            ItemConstants.Category.Material => "Item_Category_Material",
            ItemConstants.Category.Tool => "Item_Category_Tool",
            ItemConstants.Category.Seed => "Item_Category_Seed",
            ItemConstants.Category.Vegetable => "Item_Category_Vegetable",
            ItemConstants.Category.Grain => "Item_Category_Grain",
            ItemConstants.Category.Wood => "Item_Category_Wood",
            ItemConstants.Category.Ore => "Item_Category_Ore",
            ItemConstants.Category.Remnant => "Item_Category_Remnant",
            ItemConstants.Category.SkillBook => "Item_Category_SkillBook",
            _ => string.Empty
        };

        public static string SubCategory(ItemConstants.SubCategory value) => value switch
        {
            ItemConstants.SubCategory.Sword => "Item_SubCategory_Sword",
            ItemConstants.SubCategory.Chest => "Item_SubCategory_Chest",
            ItemConstants.SubCategory.Boots => "Item_SubCategory_Boots",
            ItemConstants.SubCategory.RecoverHp => "Item_SubCategory_RecoverHp",
            ItemConstants.SubCategory.RecoverMp => "Item_SubCategory_RecoverMp",
            ItemConstants.SubCategory.IncreaseAttackSpeed => "Item_SubCategory_IncreaseAttackSpeed",
            ItemConstants.SubCategory.IncreaseMoveSpeed => "Item_SubCategory_IncreaseMoveSpeed",
            ItemConstants.SubCategory.Axe => "Item_SubCategory_Axe",
            ItemConstants.SubCategory.Hoe => "Item_SubCategory_Hoe",
            ItemConstants.SubCategory.PickAxe => "Item_SubCategory_PickAxe",
            ItemConstants.SubCategory.Sickle => "Item_SubCategory_Sickle",
            ItemConstants.SubCategory.Watering => "Item_SubCategory_Watering",
            ItemConstants.SubCategory.HandHarvestable => "Item_SubCategory_HandHarvestable",
            ItemConstants.SubCategory.ScytheHarvestable => "Item_SubCategory_ScytheHarvestable",
            ItemConstants.SubCategory.IncreaseHp => "Item_SubCategory_IncreaseHp",
            ItemConstants.SubCategory.Active => "Item_SubCategory_Active",
            ItemConstants.SubCategory.Passive => "Item_SubCategory_Passive",
            ItemConstants.SubCategory.ActiveBuff => "Item_SubCategory_ActiveBuff",
            ItemConstants.SubCategory.Status => "Item_SubCategory_Status",
            _ => string.Empty
        };

        public static string Class(ItemConstants.Class value) => value switch
        {
            ItemConstants.Class.Normal => "Item_Class_Normal",
            ItemConstants.Class.Magic => "Item_Class_Magic",
            ItemConstants.Class.Rare => "Item_Class_Rare",
            ItemConstants.Class.Unique => "Item_Class_Unique",
            _ => string.Empty
        };

        public static string AntiFlag(ItemConstants.AntiFlag value) => value switch
        {
            ItemConstants.AntiFlag.ShopSale => "Item_AntiFlag_ShopSale",
            ItemConstants.AntiFlag.Stash => "Item_AntiFlag_Stash",
            ItemConstants.AntiFlag.Salvage => "Item_AntiFlag_Salvage",
            ItemConstants.AntiFlag.Upgrade => "Item_AntiFlag_Upgrade",
            _ => string.Empty
        };
    }
}
