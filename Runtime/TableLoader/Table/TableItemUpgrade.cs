using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 강화 테이블 Structure
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
    /// 아이템 강화 테이블
    /// </summary>
    public class TableItemUpgrade : DefaultTable<StruckTableItemUpgrade>
    {
        public override string Key => ConfigAddressableTable.ItemUpgrade;
        private readonly Dictionary<int, StruckTableItemUpgrade> _dictionaryByItemUid = new Dictionary<int, StruckTableItemUpgrade>();

        protected override void OnLoadedData(StruckTableItemUpgrade data)
        {
            _dictionaryByItemUid.TryAdd(data.SourceItemUid, data);
        }

        public StruckTableItemUpgrade GetDataBySourceItemUid(int sourceItemUid)
        {
            return _dictionaryByItemUid.GetValueOrDefault(sourceItemUid);
        }

        protected override StruckTableItemUpgrade BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableItemUpgrade
            {
                Uid = reader.Int("Uid"),
                Memo = reader.String("Memo"),
                SourceItemUid = reader.Int("SourceItemUid"),
                ResultItemUid = reader.Int("ResultItemUid"),
                Upgrade = reader.Int("Upgrade"),
                MaxUpgrade = reader.Int("MaxUpgrade"),
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
    }
}