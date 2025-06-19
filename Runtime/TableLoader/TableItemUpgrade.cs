
using System.Collections.Generic;

namespace GGemCo.Scripts
{
    /// <summary>
    /// Window 테이블 Structure
    /// </summary>
    public class StruckTableItemUpgrade
    {
        public int Uid;
        public string Memo;
        public int SourceItemUid;
        public int ResultItemUid;
        public int Upgrade;
        public int MaxUpgrade;
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
    /// Window 테이블
    /// </summary>
    public class TableItemUpgrade : DefaultTable
    {
        private readonly Dictionary<int, StruckTableItemUpgrade> dictionaryByItemUid = new Dictionary<int, StruckTableItemUpgrade>();

        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);
            
            StruckTableItemUpgrade struckTableItemUpgrade = GetDataByUid(uid);
            dictionaryByItemUid.TryAdd(struckTableItemUpgrade.SourceItemUid, struckTableItemUpgrade);
        }

        public StruckTableItemUpgrade GetDataBySourceItemUid(int sourceItemUid)
        {
            return dictionaryByItemUid.GetValueOrDefault(sourceItemUid);
        }

        public StruckTableItemUpgrade GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }

            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableItemUpgrade
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                SourceItemUid = int.Parse(data["SourceItemUid"]),
                ResultItemUid = int.Parse(data["ResultItemUid"]),
                Upgrade = int.Parse(data["Upgrade"]),
                MaxUpgrade = int.Parse(data["MaxUpgrade"]),
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
        }
    }
}