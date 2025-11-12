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
        public int ItemUid;
        public CurrencyConstants.Type CurrencyType;
        public int CurrencyValue;
        public int MaxBuyCount;
    }
    /// <summary>
    /// 상점 판매 테이블
    /// </summary>
    public class TableShop : DefaultTable<StruckTableShop>
    {
        public override string Key => ConfigAddressableTable.Shop;
        private readonly Dictionary<int, List<StruckTableShop>> _shopItems = new Dictionary<int, List<StruckTableShop>>();
        protected override void OnLoadedData(StruckTableShop data)
        {
            int uid = data.Uid;

            if (!_shopItems.ContainsKey(uid))
            {
                _shopItems.TryAdd(uid, new List<StruckTableShop>());
            }

            _shopItems[uid].Add(data);
        }
        protected override StruckTableShop BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableShop
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Memo = data["Memo"],
                ItemUid = MathHelper.ParseInt(data["ItemUid"]),
                CurrencyType = ConvertCurrencyType(data["CurrencyType"]),
                CurrencyValue = MathHelper.ParseInt(data["CurrencyValue"]),
                MaxBuyCount = MathHelper.ParseInt(data["MaxBuyCount"]),
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