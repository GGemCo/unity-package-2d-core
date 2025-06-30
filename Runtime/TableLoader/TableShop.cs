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
    public class TableShop : DefaultTable
    {
        private readonly Dictionary<int, List<StruckTableShop>> _shopItems = new Dictionary<int, List<StruckTableShop>>();
        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);

            if (!_shopItems.ContainsKey(uid))
            {
                _shopItems.TryAdd(uid, new List<StruckTableShop>());
            }

            StruckTableShop struckTableShop = new StruckTableShop
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                ItemUid = int.Parse(data["ItemUid"]),
                CurrencyType = ConvertCurrencyType(data["CurrencyType"]),
                CurrencyValue = int.Parse(data["CurrencyValue"]),
                MaxBuyCount = int.Parse(data["MaxBuyCount"]),
            };
            _shopItems[uid].Add(struckTableShop);
        }
        
        public List<StruckTableShop> GetDataByUid(int uid)
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