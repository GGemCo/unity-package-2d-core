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

            if (!_shopItems.ContainsKey(uid))
            {
                _shopItems.TryAdd(uid, new List<StruckTableShop>());
            }

            if (data.SlotIndex < 0)
            {
                data.SlotIndex = _shopItems[uid].Count;
            }

            _shopItems[uid].Add(data);
        }
        protected override StruckTableShop BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableShop
            {
                Uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid")),
                Memo = data.GetValueOrDefault("Memo"),
                SlotIndex = MathHelper.ParseInt(data.GetValueOrDefault("SlotIndex"), -1),
                ItemUid = MathHelper.ParseInt(data.GetValueOrDefault("ItemUid")),
                CurrencyType = ConvertCurrencyType(data.GetValueOrDefault("CurrencyType")),
                CurrencyValue = MathHelper.ParseInt(data.GetValueOrDefault("CurrencyValue")),
                MaxBuyCount = MathHelper.ParseInt(data.GetValueOrDefault("MaxBuyCount")),
                Rate = MathHelper.ParseInt(data.GetValueOrDefault("Rate"), 100),
                UniqueGroup = MathHelper.ParseInt(data.GetValueOrDefault("UniqueGroup")),
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
