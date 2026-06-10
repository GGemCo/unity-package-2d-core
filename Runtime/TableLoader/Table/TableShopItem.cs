using System.Collections.Generic;

namespace GGemCo2DCore
{
    public enum ShopSoldOutDisplayType
    {
        Disable = 0,
        Hide = 1,
    }

    /// <summary>
    /// 상점 아이템 구매 후 처리 정책입니다.
    /// </summary>
    public enum ShopBuyUsePolicy
    {
        /// <summary>구매한 아이템을 인벤토리에 추가합니다.</summary>
        AddToInventory = 0,

        /// <summary>구매한 아이템을 인벤토리에 넣지 않고 즉시 사용합니다.</summary>
        UseImmediately = 1,
    }

    /// <summary>
    /// Sale item row displayed by a shop.
    /// </summary>
    public sealed class StruckTableShopItem
    {
        public int Uid;
        public int ShopUid;
        public string Memo;
        public int SlotIndex;
        public int ItemUid;
        public CurrencyConstants.Type CurrencyType;
        public int CurrencyValue;
        public int MaxBuyCount;
        public int Rate;
        public int UniqueGroup;
        public int PurchaseLimitCount;
        public ShopSoldOutDisplayType SoldOutDisplayType;

        /// <summary>
        /// 구매 성공 후 아이템을 인벤토리에 넣을지, 즉시 사용할지 결정하는 정책입니다.
        /// </summary>
        public ShopBuyUsePolicy BuyUsePolicy;

        public static StruckTableShopItem FromLegacyShopRow(StruckTableShop row)
        {
            if (row == null) return null;

            return new StruckTableShopItem
            {
                Uid = StableLegacyUid(row),
                ShopUid = row.Uid,
                Memo = row.Memo,
                SlotIndex = row.SlotIndex,
                ItemUid = row.ItemUid,
                CurrencyType = row.CurrencyType,
                CurrencyValue = row.CurrencyValue,
                MaxBuyCount = row.MaxBuyCount,
                Rate = row.Rate,
                UniqueGroup = row.UniqueGroup,
                PurchaseLimitCount = 0,
                SoldOutDisplayType = ShopSoldOutDisplayType.Disable,
                BuyUsePolicy = ShopBuyUsePolicy.AddToInventory,
            };
        }

        private static int StableLegacyUid(StruckTableShop row)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + row.Uid;
                hash = hash * 31 + row.SlotIndex;
                hash = hash * 31 + row.ItemUid;
                hash = hash * 31 + (int)row.CurrencyType;
                hash = hash * 31 + row.CurrencyValue;
                return hash < 0 ? -hash : hash;
            }
        }
    }

    /// <summary>
    /// shop_item table.
    /// </summary>
    public sealed class TableShopItem : DefaultTable<StruckTableShopItem>
    {
        public override string Key => ConfigAddressableTable.ShopItem;

        private readonly Dictionary<int, List<StruckTableShopItem>> _itemsByShopUid = new Dictionary<int, List<StruckTableShopItem>>();

        protected override void PreLoad()
        {
            base.PreLoad();
            _itemsByShopUid.Clear();
        }

        protected override void OnLoadedData(StruckTableShopItem data)
        {
            if (data == null || data.ShopUid <= 0) return;

            if (!_itemsByShopUid.TryGetValue(data.ShopUid, out var items))
            {
                items = new List<StruckTableShopItem>();
                _itemsByShopUid.Add(data.ShopUid, items);
            }

            if (data.SlotIndex < 0) data.SlotIndex = items.Count;
            items.Add(data);
        }

        protected override StruckTableShopItem BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableShopItem
            {
                Uid = reader.Int("Uid"),
                ShopUid = reader.Int("ShopUid"),
                Memo = reader.String("Memo"),
                SlotIndex = reader.Int("SlotIndex", -1),
                ItemUid = reader.Int("ItemUid"),
                CurrencyType = ConvertCurrencyType(reader.String("CurrencyType")),
                CurrencyValue = reader.Int("CurrencyValue"),
                MaxBuyCount = reader.Int("MaxBuyCount", 1),
                Rate = reader.Int("Rate", 100),
                UniqueGroup = reader.Int("UniqueGroup"),
                PurchaseLimitCount = reader.Int("PurchaseLimitCount"),
                SoldOutDisplayType = reader.Enum<ShopSoldOutDisplayType>("SoldOutDisplayType"),
                BuyUsePolicy = reader.Enum<ShopBuyUsePolicy>("BuyUsePolicy"),
            };
        }

        public List<StruckTableShopItem> GetItemsByShopUid(int shopUid)
        {
            if (shopUid <= 0)
            {
                GcLogger.LogError("shopUid is 0.");
                return null;
            }

            return _itemsByShopUid.GetValueOrDefault(shopUid);
        }
    }
}
