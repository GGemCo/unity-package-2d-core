
using System.Collections.Generic;

namespace GGemCo.Scripts
{
    /// <summary>
    /// Window 테이블 Structure
    /// </summary>
    public class StruckTableItemSalvage
    {
        public int Uid;
        public string Memo;
        public int SourceItemUid;
        public CurrencyConstants.Type NeedCurrencyType;
        public int NeedCurrencyValue;
        public int ResultItemUid;
        public int ResultItemCount;
    }

    /// <summary>
    /// Window 테이블
    /// </summary>
    public class TableItemSalvage : DefaultTable
    {
        private readonly Dictionary<int, StruckTableItemSalvage> dictionaryByItemUid = new Dictionary<int, StruckTableItemSalvage>();

        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);
            
            StruckTableItemSalvage struckTableItemSalvage = GetDataByUid(uid);
            dictionaryByItemUid.TryAdd(struckTableItemSalvage.SourceItemUid, struckTableItemSalvage);
        }

        public StruckTableItemSalvage GetDataBySourceItemUid(int sourceItemUid)
        {
            return dictionaryByItemUid.GetValueOrDefault(sourceItemUid);
        }

        private StruckTableItemSalvage GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }

            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableItemSalvage
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                SourceItemUid = int.Parse(data["SourceItemUid"]),
                NeedCurrencyType = ConvertCurrencyType(data["NeedCurrencyType"]),
                NeedCurrencyValue = int.Parse(data["NeedCurrencyValue"]),
                ResultItemUid = int.Parse(data["ResultItemUid"]),
                ResultItemCount = int.Parse(data["ResultItemCount"]),
            };
        }
    }
}