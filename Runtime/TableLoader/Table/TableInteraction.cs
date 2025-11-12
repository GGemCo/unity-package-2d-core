using System.Collections.Generic;

namespace GGemCo2DCore
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
    /// Interaction 테이블
    /// </summary>
    public class TableInteraction : DefaultTable<StruckTableInteraction>
    {
        public override string Key => ConfigAddressableTable.Interaction;

        protected override StruckTableInteraction BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableInteraction
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Memo = data["Memo"],
                Message = data["Message"],
                Type1 = EnumHelper.ConvertEnum<InteractionConstants.Type>(data["Type1"]),
                Value1 = MathHelper.ParseInt(data["Value1"]),
                Type2 = EnumHelper.ConvertEnum<InteractionConstants.Type>(data["Type2"]),
                Value2 = MathHelper.ParseInt(data["Value2"]),
                Type3 = EnumHelper.ConvertEnum<InteractionConstants.Type>(data["Type3"]),
                Value3 = MathHelper.ParseInt(data["Value3"]),
            };
        }
    }
}