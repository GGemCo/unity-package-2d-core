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
        public int DialogueUid;
        public string DialogueStartNodeGuid;
        public string DialogueUidRandomList;
        public string DialogueStartNodeGuidRandomList;
        public InteractionDialogueEndPolicy DialogueEndPolicy;
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

        /// <summary>
        /// interaction row를 런타임 구조체로 변환합니다.
        /// 신규 dialogue 컬럼이 없더라도 안전하게 기본값으로 동작합니다.
        /// </summary>
        /// <param name="data">원본 테이블 row 데이터입니다.</param>
        /// <returns>변환된 interaction row입니다.</returns>
        protected override StruckTableInteraction BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableInteraction
            {
                Uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid")),
                Memo = data.GetValueOrDefault("Memo"),
                Message = data.GetValueOrDefault("Message"),
                DialogueUid = MathHelper.ParseInt(data.GetValueOrDefault("DialogueUid")),
                DialogueStartNodeGuid = data.GetValueOrDefault("DialogueStartNodeGuid"),
                DialogueUidRandomList = data.GetValueOrDefault("DialogueUidRandomList"),
                DialogueStartNodeGuidRandomList = data.GetValueOrDefault("DialogueStartNodeGuidRandomList"),
                DialogueEndPolicy = EnumHelper.ConvertEnum<InteractionDialogueEndPolicy>(
                    data.GetValueOrDefault("DialogueEndPolicy")),
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
