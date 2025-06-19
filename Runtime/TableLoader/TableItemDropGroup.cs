using System.Collections.Generic;

namespace GGemCo.Scripts
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
    public class TableItemDropGroup : DefaultTable
    {
        private static readonly Dictionary<string, ItemManager.ItemDropGroup> MapType;

        static TableItemDropGroup()
        {
            MapType = new Dictionary<string, ItemManager.ItemDropGroup>
            {
                { "ItemCategory", ItemManager.ItemDropGroup.ItemCategory },
                { "ItemSubCategory", ItemManager.ItemDropGroup.ItemSubCategory },
                { "ItemUid", ItemManager.ItemDropGroup.ItemUid },
                { "ExcludeItemUid", ItemManager.ItemDropGroup.ExcludeItemUid },
                { "Nothing", ItemManager.ItemDropGroup.Nothing },
            };
        }
        private static ItemManager.ItemDropGroup ConvertType(string type) => MapType.GetValueOrDefault(type, ItemManager.ItemDropGroup.None);

        private readonly Dictionary<int, List<StruckTableItemDropGroup>> dropGroupDictionary = new Dictionary<int, List<StruckTableItemDropGroup>>();
        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);

            if (!dropGroupDictionary.ContainsKey(uid))
            {
                dropGroupDictionary[uid] = new List<StruckTableItemDropGroup>();
            }
            StruckTableItemDropGroup struckTableItemDropGroup = new StruckTableItemDropGroup
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                Type = ConvertType(data["Type"]),
                Value = data["Value"],
                Rate = int.Parse(data["Rate"]),
            };
            dropGroupDictionary[uid].Add(struckTableItemDropGroup);
        }

        public Dictionary<int, List<StruckTableItemDropGroup>> GetDropGroups()
        {
            return dropGroupDictionary;
        }
    }
}