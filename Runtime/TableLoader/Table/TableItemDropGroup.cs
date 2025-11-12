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
            return new StruckTableItemDropGroup
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Memo = data["Memo"],
                Type = EnumHelper.ConvertEnum<ItemManager.ItemDropGroup>(data["Type"]),
                Value = data["Value"],
                Rate = MathHelper.ParseInt(data["Rate"]),
            };
        }
        public Dictionary<int, List<StruckTableItemDropGroup>> GetDropGroups()
        {
            return _dropGroupDictionary;
        }
    }
}