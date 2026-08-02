using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 품절된 상점 상품을 현재 슬롯에 표시하는 방식을 정의합니다.
    /// </summary>
    public enum ShopSoldOutDisplayType
    {
        /// <summary>현재 상품을 품절 상태로 표시하고 구매를 비활성화합니다.</summary>
        Disable = 0,

        /// <summary>현재 상품을 상점 목록에서 숨깁니다.</summary>
        Hide = 1,
    }

    /// <summary>
    /// 품절된 상점 상품이 다음 슬롯 추첨에 참여할지 결정합니다.
    /// 현재 표시 정책과 다음 재추첨 후보 정책을 분리하여 판매 목록이 구매 직후 변경되지 않도록 합니다.
    /// </summary>
    public enum ShopSoldOutRollPolicy
    {
        /// <summary>품절 상태여도 다음 추첨 후보에 유지합니다.</summary>
        KeepCandidate = 0,

        /// <summary>품절 상태이면 다음 추첨 후보에서 제외합니다.</summary>
        ExcludeCandidate = 1,
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

        /// <summary>
        /// 동일 슬롯 후보 중 먼저 추첨할 우선순위입니다.
        /// 가장 높은 값의 후보군만 가중치 추첨에 참여하며, 값이 같으면 <see cref="Rate"/>를 사용합니다.
        /// </summary>
        public int RollPriority;

        public int UniqueGroup;
        public int PurchaseLimitCount;
        public ShopSoldOutDisplayType SoldOutDisplayType;

        /// <summary>
        /// 재고가 모두 소진된 뒤 다음 슬롯 추첨에서 후보를 유지할지 결정하는 정책입니다.
        /// </summary>
        public ShopSoldOutRollPolicy SoldOutRollPolicy;

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
                RollPriority = 0,
                UniqueGroup = row.UniqueGroup,
                PurchaseLimitCount = 0,
                SoldOutDisplayType = ShopSoldOutDisplayType.Disable,
                SoldOutRollPolicy = ShopSoldOutRollPolicy.KeepCandidate,
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
                RollPriority = reader.Int("RollPriority"),
                UniqueGroup = reader.Int("UniqueGroup"),
                PurchaseLimitCount = reader.Int("PurchaseLimitCount"),
                SoldOutDisplayType = reader.Enum<ShopSoldOutDisplayType>("SoldOutDisplayType"),
                SoldOutRollPolicy = reader.Enum<ShopSoldOutRollPolicy>("SoldOutRollPolicy"),
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
