using System.Collections.Generic;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 드랍 확률 테이블 Structure
    /// </summary>
    public class StruckTableMonsterDropRate
    {
        public int Uid;
        public string Memo;
        public int MonsterUid;
        public ItemManager.MonsterDropRateType Type;
        public int Value;
        public int Rate;
    }
    /// <summary>
    /// 아이템 드랍 확률 테이블
    /// </summary>
    public class TableMonsterDropRate : DefaultTable
    {
        private static readonly Dictionary<string, ItemManager.MonsterDropRateType> MapType;

        static TableMonsterDropRate()
        {
            MapType = new Dictionary<string, ItemManager.MonsterDropRateType>
            {
                { "ItemDropGroupUid", ItemManager.MonsterDropRateType.ItemDropGroupUid },
                { "Nothing", ItemManager.MonsterDropRateType.Nothing },
            };
        }
        private static ItemManager.MonsterDropRateType ConvertType(string type) =>
            MapType.GetValueOrDefault(type, ItemManager.MonsterDropRateType.None);

        private readonly Dictionary<int, List<StruckTableMonsterDropRate>> monsterDropDictionary =
            new Dictionary<int, List<StruckTableMonsterDropRate>>();

        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);
            int monsterUid = int.Parse(data["MonsterUid"]);

            if (!monsterDropDictionary.ContainsKey(monsterUid))
            {
                monsterDropDictionary[monsterUid] = new List<StruckTableMonsterDropRate>();
            }

            StruckTableMonsterDropRate struckTableMonsterDropRate = GetDataByUid(uid);
            monsterDropDictionary[monsterUid].Add(struckTableMonsterDropRate);
        }

        public Dictionary<int, List<StruckTableMonsterDropRate>> GetMonsterDropDictionary()
        {
            return monsterDropDictionary;
        }

        private StruckTableMonsterDropRate GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableMonsterDropRate
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                MonsterUid = int.Parse(data["MonsterUid"]),
                Type = ConvertType(data["Type"]),
                Value = int.Parse(data["Value"]),
                Rate = int.Parse(data["Rate"]),
            };
        }
    }
}