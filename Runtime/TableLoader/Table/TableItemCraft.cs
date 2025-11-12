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
            return new StruckTableItemCraft
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Memo = data["Memo"],
                ResultItemUid = MathHelper.ParseInt(data["ResultItemUid"]),
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