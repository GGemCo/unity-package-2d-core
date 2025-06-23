using System.Collections.Generic;

namespace GGemCo.Scripts
{
    public abstract class InteractionConstants
    {
        public enum Type
        {
            None,
            Shop,
            ShopSale,
            ItemUpgrade,
            ItemSalvage,
            Stash,
            ItemCraft,
        }

        private static readonly Dictionary<Type, string> DictionaryTypeName = new Dictionary<Type, string>
        {
            { Type.None, "" },
            { Type.Shop, "Buy Item" }, // 아이템 구매
            { Type.ShopSale, "Sell Item" }, //아이템 판매
            { Type.ItemUpgrade, "Item Upgrade" }, // 아이템 강화
            { Type.ItemSalvage, "Item Salvage" },//아이템 분해
            { Type.Stash, "Stash" }, //창고
            { Type.ItemCraft, "Item Craft" },//아이템 제작
        };

        public static string GetTypeName(Type type)
        {
            return DictionaryTypeName[type];
        }
    }
}