using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 제작 테이블 Structure
    /// </summary>
    public class StruckTableItemCraft
    {
        public int Uid;
        public string Memo;
        public int ResultItemUid;
        public int Rate;
        public CurrencyConstants.Type NeedCurrencyType;
        public int NeedCurrencyValue;
        public int NeedItemUid1;
        public int NeedItemCount1;
        public int NeedItemUid2;
        public int NeedItemCount2;
        public int NeedItemUid3;
        public int NeedItemCount3;
        public int NeedItemUid4;
        public int NeedItemCount4;
    }

    /// <summary>
    /// 아이템 제작 테이블
    /// </summary>
    public class TableItemCraft : DefaultTable
    {
        private readonly Dictionary<int, List<StruckTableItemCraft>> _craftItems = new Dictionary<int, List<StruckTableItemCraft>>();
        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);

            if (!_craftItems.ContainsKey(uid))
            {
                _craftItems.TryAdd(uid, new List<StruckTableItemCraft>());
            }

            StruckTableItemCraft struckTableItemCraft = new StruckTableItemCraft
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                ResultItemUid = int.Parse(data["ResultItemUid"]),
                Rate = int.Parse(data["Rate"]),
                NeedCurrencyType = ConvertCurrencyType(data["NeedCurrencyType"]),
                NeedCurrencyValue = int.Parse(data["NeedCurrencyValue"]),
                NeedItemUid1 = int.Parse(data["NeedItemUid1"]),
                NeedItemCount1 = int.Parse(data["NeedItemCount1"]),
                NeedItemUid2 = int.Parse(data["NeedItemUid2"]),
                NeedItemCount2 = int.Parse(data["NeedItemCount2"]),
                NeedItemUid3 = int.Parse(data["NeedItemUid3"]),
                NeedItemCount3 = int.Parse(data["NeedItemCount3"]),
                NeedItemUid4 = int.Parse(data["NeedItemUid4"]),
                NeedItemCount4 = int.Parse(data["NeedItemCount4"]),
            };
            _craftItems[uid].Add(struckTableItemCraft);
        }
        public List<StruckTableItemCraft> GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }

            return _craftItems.GetValueOrDefault(uid);
        }
    }
}