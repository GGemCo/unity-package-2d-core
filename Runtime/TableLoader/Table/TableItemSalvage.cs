using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 분해 테이블 Structure
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
    /// 아이템 분해 테이블
    /// </summary>
    public class TableItemSalvage : DefaultTable<StruckTableItemSalvage>
    {
        public override string Key => ConfigAddressableTable.ItemSalvage;
        private readonly Dictionary<int, StruckTableItemSalvage> _dictionaryByItemUid = new Dictionary<int, StruckTableItemSalvage>();

        protected override void OnLoadedData(StruckTableItemSalvage data)
        {
            _dictionaryByItemUid.TryAdd(data.SourceItemUid, data);
        }

        public StruckTableItemSalvage GetDataBySourceItemUid(int sourceItemUid)
        {
            return _dictionaryByItemUid.GetValueOrDefault(sourceItemUid);
        }

        protected override StruckTableItemSalvage BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableItemSalvage
            {
                Uid = reader.Int("Uid"),
                Memo = reader.String("Memo"),
                SourceItemUid = reader.Int("SourceItemUid"),
                NeedCurrencyType = ConvertCurrencyType(reader.String("NeedCurrencyType")),
                NeedCurrencyValue = reader.Int("NeedCurrencyValue"),
                ResultItemUid = reader.Int("ResultItemUid"),
                ResultItemCount = reader.Int("ResultItemCount"),
            };
        }
    }
}