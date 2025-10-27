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
    public class TableNpcDropRate : DefaultTable
    {
        private static readonly Dictionary<string, ItemManager.DropRateType> MapType;

        static TableNpcDropRate()
        {
            MapType = new Dictionary<string, ItemManager.DropRateType>
            {
                { "ItemDropGroupUid", ItemManager.DropRateType.ItemDropGroupUid },
                { "Nothing", ItemManager.DropRateType.Nothing },
            };
        }
        private static ItemManager.DropRateType ConvertType(string type) =>
            MapType.GetValueOrDefault(type, ItemManager.DropRateType.None);

        private readonly Dictionary<int, List<StruckTableNpcDropRate>> npcDropDictionary =
            new Dictionary<int, List<StruckTableNpcDropRate>>();

        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);
            int npcUid = int.Parse(data["NpcUid"]);

            if (!npcDropDictionary.ContainsKey(npcUid))
            {
                npcDropDictionary[npcUid] = new List<StruckTableNpcDropRate>();
            }

            StruckTableNpcDropRate struckTableNpcDropRate = GetDataByUid(uid);
            npcDropDictionary[npcUid].Add(struckTableNpcDropRate);
        }

        public Dictionary<int, List<StruckTableNpcDropRate>> GetNpcDropDictionary()
        {
            return npcDropDictionary;
        }

        private StruckTableNpcDropRate GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableNpcDropRate
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                NpcUid = int.Parse(data["NpcUid"]),
                Type = ConvertType(data["Type"]),
                Value = int.Parse(data["Value"]),
                Rate = int.Parse(data["Rate"]),
            };
        }
    }
}