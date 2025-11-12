using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 드랍 확률 테이블 Structure
    /// </summary>
    public class StruckTableNpcDropRate
    {
        public int Uid;
        public string Memo;
        public int NpcUid;
        public ItemManager.DropRateType Type;
        public int Value;
        public int Rate;
    }
    /// <summary>
    /// 아이템 드랍 확률 테이블
    /// </summary>
    public class TableNpcDropRate : DefaultTable<StruckTableNpcDropRate>
    {
        public override string Key => ConfigAddressableTable.NpcDropRate;
        private readonly Dictionary<int, List<StruckTableNpcDropRate>> _npcDropDictionary =
            new Dictionary<int, List<StruckTableNpcDropRate>>();

        protected override void OnLoadedData(StruckTableNpcDropRate data)
        {
            int uid = data.Uid;
            int npcUid = data.NpcUid;

            if (!_npcDropDictionary.ContainsKey(npcUid))
            {
                _npcDropDictionary[npcUid] = new List<StruckTableNpcDropRate>();
            }

            _npcDropDictionary[npcUid].Add(data);
        }

        public Dictionary<int, List<StruckTableNpcDropRate>> GetNpcDropDictionary()
        {
            return _npcDropDictionary;
        }

        protected override StruckTableNpcDropRate BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableNpcDropRate
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Memo = data["Memo"],
                NpcUid = MathHelper.ParseInt(data["NpcUid"]),
                Type = EnumHelper.ConvertEnum<ItemManager.DropRateType>(data["Type"]),
                Value = MathHelper.ParseInt(data["Value"]),
                Rate = MathHelper.ParseInt(data["Rate"]),
            };
        }
    }
}