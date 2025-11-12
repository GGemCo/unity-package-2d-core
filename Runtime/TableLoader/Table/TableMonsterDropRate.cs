using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 드랍 확률 테이블 Structure
    /// </summary>
    public class StruckTableMonsterDropRate
    {
        public int Uid;
        public string Memo;
        public int MonsterUid;
        public ItemManager.DropRateType Type;
        public int Value;
        public int Rate;
    }
    /// <summary>
    /// 아이템 드랍 확률 테이블
    /// </summary>
    public class TableMonsterDropRate : DefaultTable<StruckTableMonsterDropRate>
    {
        public override string Key => ConfigAddressableTable.MonsterDropRate;

        private readonly Dictionary<int, List<StruckTableMonsterDropRate>> _monsterDropDictionary =
            new Dictionary<int, List<StruckTableMonsterDropRate>>();

        protected override void OnLoadedData(StruckTableMonsterDropRate data)
        {
            int monsterUid = data.MonsterUid;

            if (!_monsterDropDictionary.ContainsKey(monsterUid))
            {
                _monsterDropDictionary[monsterUid] = new List<StruckTableMonsterDropRate>();
            }
            _monsterDropDictionary[monsterUid].Add(data);
        }

        public Dictionary<int, List<StruckTableMonsterDropRate>> GetMonsterDropDictionary()
        {
            return _monsterDropDictionary;
        }

        protected override StruckTableMonsterDropRate BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableMonsterDropRate
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                MonsterUid = int.Parse(data["MonsterUid"]),
                Type = EnumHelper.ConvertEnum<ItemManager.DropRateType>(data["Type"]),
                Value = int.Parse(data["Value"]),
                Rate = int.Parse(data["Rate"]),
            };
        }
    }
}