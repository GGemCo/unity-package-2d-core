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
            return new StruckTableItemUpgrade
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Memo = data["Memo"],
                SourceItemUid = MathHelper.ParseInt(data["SourceItemUid"]),
                ResultItemUid = MathHelper.ParseInt(data["ResultItemUid"]),
                Upgrade = MathHelper.ParseInt(data["Upgrade"]),
                MaxUpgrade = MathHelper.ParseInt(data["MaxUpgrade"]),
                Rate = MathHelper.ParseInt(data["Rate"]),
                NeedCurrencyType = ConvertCurrencyType(data["NeedCurrencyType"]),
                NeedCurrencyValue = MathHelper.ParseInt(data["NeedCurrencyValue"]),
                NeedItemUid1 = MathHelper.ParseInt(data["NeedItemUid1"]),
                NeedItemCount1 = MathHelper.ParseInt(data["NeedItemCount1"]),
                NeedItemUid2 = MathHelper.ParseInt(data["NeedItemUid2"]),
                NeedItemCount2 = MathHelper.ParseInt(data["NeedItemCount2"]),
                NeedItemUid3 = MathHelper.ParseInt(data["NeedItemUid3"]),
                NeedItemCount3 = MathHelper.ParseInt(data["NeedItemCount3"]),
                NeedItemUid4 = MathHelper.ParseInt(data["NeedItemUid4"]),
                NeedItemCount4 = MathHelper.ParseInt(data["NeedItemCount4"]),
            };
        }
    }
}