using System.Collections.Generic;

namespace GGemCo.Scripts
{
    /// <summary>
    /// Shop 테이블 Structure
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
    /// Npc 테이블
    /// </summary>
    public class TableShop : DefaultTable
    {
        // 레벨 1인 것만 모아놓은 dictionary
        private readonly Dictionary<int, List<StruckTableShop>> shopItems = new Dictionary<int, List<StruckTableShop>>();
        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);

            if (!shopItems.ContainsKey(uid))
            {
                shopItems.TryAdd(uid, new List<StruckTableShop>());
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
            shopItems[uid].Add(struckTableShop);
        }
        
        public List<StruckTableShop> GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }

            return shopItems.GetValueOrDefault(uid);
        }
    }
}