using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 상점 판매 테이블 Structure
    /// </summary>
    public class StruckTableShop
    {
        public int Uid;
        public string Memo;
        public string Name;

        // Legacy item columns kept for projects that still read sale rows from shop.txt.
        public bool IsLegacyItemRow;
        public int SlotIndex;
        public int ItemUid;
        public CurrencyConstants.Type CurrencyType;
        public int CurrencyValue;
        public int MaxBuyCount;
        public int Rate;
        public int UniqueGroup;
    }
    /// <summary>
    /// 상점 판매 테이블
    /// </summary>
    public class TableShop : DefaultTable<StruckTableShop>
    {
        public override string Key => ConfigAddressableTable.Shop;
        private readonly Dictionary<int, List<StruckTableShop>> _shopItems = new Dictionary<int, List<StruckTableShop>>();

        protected override void PreLoad()
        {
            _shopItems.Clear();
        }

        protected override void OnLoadedData(StruckTableShop data)
        {
            int uid = data.Uid;

            if (!data.IsLegacyItemRow) return;

            if (!_shopItems.ContainsKey(uid))
            {
                _shopItems.TryAdd(uid, new List<StruckTableShop>());
            }

            if (data.SlotIndex < 0) data.SlotIndex = _shopItems[uid].Count;
            _shopItems[uid].Add(data);
        }
        protected override StruckTableShop BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableShop
            {
                Uid = reader.Int("Uid"),
                Memo = reader.String("Memo"),
                Name = reader.String("Name"),
                IsLegacyItemRow = data.ContainsKey("ItemUid") || data.ContainsKey("SlotIndex"),
                SlotIndex = reader.Int("SlotIndex", -1),
                ItemUid = reader.Int("ItemUid"),
                CurrencyType = data.ContainsKey("CurrencyType")
                    ? ConvertCurrencyType(reader.String("CurrencyType"))
                    : CurrencyConstants.Type.None,
                CurrencyValue = reader.Int("CurrencyValue"),
                MaxBuyCount = reader.Int("MaxBuyCount"),
                Rate = reader.Int("Rate", 100),
                UniqueGroup = reader.Int("UniqueGroup"),
            };
        }
        public List<StruckTableShop> GetItemByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }

            return _shopItems.GetValueOrDefault(uid);
        }
    }
}
