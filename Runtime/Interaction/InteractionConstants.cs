using System.Collections.Generic;

namespace GGemCo2DCore
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

        public static string GetTypeName(Type type)
        {
            return type switch
            {
                Type.Shop => LocalizationManager.Instance.GetUIWindowTitleByKey("UIWindowShop"), // 아이템 구매
                Type.ShopSale => LocalizationManager.Instance.GetUIWindowTitleByKey("UIWindowShopSale"), //아이템 판매
                Type.ItemUpgrade => LocalizationManager.Instance.GetUIWindowTitleByKey("UIWindowItemUpgrade"), // 아이템 강화
                Type.ItemSalvage => LocalizationManager.Instance.GetUIWindowTitleByKey("UIWindowItemSalvage"),//아이템 분해
                Type.Stash => LocalizationManager.Instance.GetUIWindowTitleByKey("UIWindowStash"), //창고
                Type.ItemCraft => LocalizationManager.Instance.GetUIWindowTitleByKey("UIWindowItemCraft"),//아이템 제작
                _ => ""
            };
        }
    }
}