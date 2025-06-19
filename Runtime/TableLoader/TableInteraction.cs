using System.Collections.Generic;

namespace GGemCo.Scripts
{
    /// <summary>
    /// Interaction 테이블 Structure
    /// </summary>
    public class StruckTableInteraction
    {
        public int Uid;
        public string Memo;
        public string Message;
        public InteractionConstants.Type Type1;
        public int Value1;
        public InteractionConstants.Type Type2;
        public int Value2;
        public InteractionConstants.Type Type3;
        public int Value3;
    }
    /// <summary>
    /// Npc 테이블
    /// </summary>
    public class TableInteraction : DefaultTable
    {
        private static readonly Dictionary<string, InteractionConstants.Type> MapType;

        static TableInteraction()
        {
            MapType = new Dictionary<string, InteractionConstants.Type>
            {
                { "Shop", InteractionConstants.Type.Shop },
                { "ShopSale", InteractionConstants.Type.ShopSale },
                { "ItemUpgrade", InteractionConstants.Type.ItemUpgrade },
                { "ItemSalvage", InteractionConstants.Type.ItemSalvage },
                { "Stash", InteractionConstants.Type.Stash },
                { "ItemCraft", InteractionConstants.Type.ItemCraft },
            };
        }

        private InteractionConstants.Type ConvertType(string grade) => MapType.GetValueOrDefault(grade, InteractionConstants.Type.None);

        public StruckTableInteraction GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableInteraction
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                Message = data["Message"],
                Type1 = ConvertType(data["Type1"]),
                Value1 = int.Parse(data["Value1"]),
                Type2 = ConvertType(data["Type2"]),
                Value2 = int.Parse(data["Value2"]),
                Type3 = ConvertType(data["Type3"]),
                Value3 = int.Parse(data["Value3"]),
            };
        }
    }
}