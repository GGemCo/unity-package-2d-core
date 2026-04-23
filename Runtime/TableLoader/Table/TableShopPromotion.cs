using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    public enum ShopPromotionStrategyType
    {
        None = 0,
        NthExposureDiscount = 1,
    }

    /// <summary>
    /// shop_promotion table row.
    /// </summary>
    public sealed class StruckTableShopPromotion
    {
        public int Uid;
        public int ShopItemUid;
        public string Memo;
        public ShopPromotionStrategyType StrategyType;
        public int TriggerExposureCount;
        public int DiscountRate;
        public int Priority;
        public bool IsEnabled;
    }

    /// <summary>
    /// shop_promotion table.
    /// </summary>
    public sealed class TableShopPromotion : DefaultTable<StruckTableShopPromotion>
    {
        public override string Key => ConfigAddressableTable.ShopPromotion;

        private readonly Dictionary<int, List<StruckTableShopPromotion>> _itemsByShopItemUid =
            new Dictionary<int, List<StruckTableShopPromotion>>();

        protected override void PreLoad()
        {
            base.PreLoad();
            _itemsByShopItemUid.Clear();
        }

        protected override StruckTableShopPromotion BuildRow(Dictionary<string, string> data)
        {
            string isEnabled = data.GetValueOrDefault("IsEnabled");
            return new StruckTableShopPromotion
            {
                Uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid")),
                ShopItemUid = MathHelper.ParseInt(data.GetValueOrDefault("ShopItemUid")),
                Memo = data.GetValueOrDefault("Memo"),
                StrategyType = EnumHelper.ConvertEnum<ShopPromotionStrategyType>(data.GetValueOrDefault("StrategyType")),
                TriggerExposureCount = Math.Max(0, MathHelper.ParseInt(data.GetValueOrDefault("TriggerExposureCount"))),
                DiscountRate = Math.Min(100, Math.Max(0, MathHelper.ParseInt(data.GetValueOrDefault("DiscountRate")))),
                Priority = MathHelper.ParseInt(data.GetValueOrDefault("Priority")),
                IsEnabled = string.IsNullOrWhiteSpace(isEnabled) || ConvertBoolean(isEnabled),
            };
        }

        protected override void OnLoadedData(StruckTableShopPromotion data)
        {
            if (data == null || data.ShopItemUid <= 0 || !data.IsEnabled) return;

            if (!_itemsByShopItemUid.TryGetValue(data.ShopItemUid, out var items))
            {
                items = new List<StruckTableShopPromotion>();
                _itemsByShopItemUid.Add(data.ShopItemUid, items);
            }

            items.Add(data);
            items.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public List<StruckTableShopPromotion> GetItemsByShopItemUid(int shopItemUid)
        {
            if (shopItemUid <= 0) return null;
            return _itemsByShopItemUid.GetValueOrDefault(shopItemUid);
        }
    }
}
