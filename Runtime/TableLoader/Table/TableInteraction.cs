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
        public string CustomTypeKey1;
        public InteractionConstants.Type Type2;
        public int Value2;
        public string CustomTypeKey2;
        public InteractionConstants.Type Type3;
        public int Value3;
        public string CustomTypeKey3;
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
                Uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid")),
                Memo = data.GetValueOrDefault("Memo"),
                Message = data.GetValueOrDefault("Message"),
                Type1 = EnumHelper.ConvertEnum<InteractionConstants.Type>(data.GetValueOrDefault("Type1")),
                Value1 = MathHelper.ParseInt(data.GetValueOrDefault("Value1")),
                CustomTypeKey1 = data.GetValueOrDefault("CustomTypeKey1"),
                Type2 = EnumHelper.ConvertEnum<InteractionConstants.Type>(data.GetValueOrDefault("Type2")),
                Value2 = MathHelper.ParseInt(data.GetValueOrDefault("Value2")),
                CustomTypeKey2 = data.GetValueOrDefault("CustomTypeKey2"),
                Type3 = EnumHelper.ConvertEnum<InteractionConstants.Type>(data.GetValueOrDefault("Type3")),
                Value3 = MathHelper.ParseInt(data.GetValueOrDefault("Value3")),
                CustomTypeKey3 = data.GetValueOrDefault("CustomTypeKey3"),
            };
        }
    }
}
