using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 드랍 아이템 그룹 Structure
    /// </summary>
    public class StruckTableItemDropGroup
    {
        public int Uid;
        public string Memo;
        public ItemManager.ItemDropGroup Type;
        public string Value;
        public int Rate;
    }
    /// <summary>
    /// 드랍 아이템 그룹 테이블
    /// </summary>
    public class TableItemDropGroup : DefaultTable<StruckTableItemDropGroup>
    {
        public override string Key => ConfigAddressableTable.ItemDropGroup;

        private readonly Dictionary<int, List<StruckTableItemDropGroup>> _dropGroupDictionary = new Dictionary<int, List<StruckTableItemDropGroup>>();
        protected override void OnLoadedData(StruckTableItemDropGroup data)
        {
            int uid = data.Uid;

            if (!_dropGroupDictionary.ContainsKey(uid))
            {
                _dropGroupDictionary[uid] = new List<StruckTableItemDropGroup>();
            }
            _dropGroupDictionary[uid].Add(data);
        }

        protected override StruckTableItemDropGroup BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableItemDropGroup
            {
                Uid = reader.Int("Uid"),
                Memo = reader.String("Memo"),
                Type = reader.Enum<ItemManager.ItemDropGroup>("Type"),
                Value = reader.String("Value"),
                Rate = reader.Int("Rate"),
            };
        }
        public Dictionary<int, List<StruckTableItemDropGroup>> GetDropGroups()
        {
            return _dropGroupDictionary;
        }
    }
}