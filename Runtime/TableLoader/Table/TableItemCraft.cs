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
    public class TableItemCraft : DefaultTable<StruckTableItemCraft>
    {
        public override string Key => ConfigAddressableTable.ItemCraft;
        
        private readonly Dictionary<int, List<StruckTableItemCraft>> _craftItems = new Dictionary<int, List<StruckTableItemCraft>>();
        protected override void OnLoadedData(StruckTableItemCraft data)
        {
            int uid = data.Uid;

            if (!_craftItems.ContainsKey(uid))
            {
                _craftItems.TryAdd(uid, new List<StruckTableItemCraft>());
            }

            _craftItems[uid].Add(data);
        }
        protected override StruckTableItemCraft BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableItemCraft
            {
                Uid = reader.Int("Uid"),
                Memo = reader.String("Memo"),
                ResultItemUid = reader.Int("ResultItemUid"),
                Rate = reader.Int("Rate"),
                NeedCurrencyType = ConvertCurrencyType(reader.String("NeedCurrencyType")),
                NeedCurrencyValue = reader.Int("NeedCurrencyValue"),
                NeedItemUid1 = reader.Int("NeedItemUid1"),
                NeedItemCount1 = reader.Int("NeedItemCount1"),
                NeedItemUid2 = reader.Int("NeedItemUid2"),
                NeedItemCount2 = reader.Int("NeedItemCount2"),
                NeedItemUid3 = reader.Int("NeedItemUid3"),
                NeedItemCount3 = reader.Int("NeedItemCount3"),
                NeedItemUid4 = reader.Int("NeedItemUid4"),
                NeedItemCount4 = reader.Int("NeedItemCount4"),
            };
        }
        public List<StruckTableItemCraft> GetItemsByUid(int uid)
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