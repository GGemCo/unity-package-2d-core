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
        /// <summary>
        /// NPC와 현재 Interaction 조합에서 처음 한 번 재생할 Dialogue UID입니다.
        /// </summary>
        public int FirstDialogueUid;
        /// <summary>
        /// 첫 Dialogue 진입 시 사용할 시작 노드 GUID입니다.
        /// </summary>
        public string FirstDialogueStartNodeGuid;
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
            TableRowReader reader = ReadRow(data);
            return new StruckTableInteraction
            {
                Uid = reader.Int("Uid"),
                Memo = reader.String("Memo"),
                Message = reader.String("Message"),
                FirstDialogueUid = reader.Int("FirstDialogueUid"),
                FirstDialogueStartNodeGuid = reader.String("FirstDialogueStartNodeGuid"),
                DialogueUid = reader.Int("DialogueUid"),
                DialogueStartNodeGuid = reader.String("DialogueStartNodeGuid"),
                DialogueUidRandomList = reader.String("DialogueUidRandomList"),
                DialogueStartNodeGuidRandomList = reader.String("DialogueStartNodeGuidRandomList"),
                DialogueEndPolicy = EnumHelper.ConvertEnum<InteractionDialogueEndPolicy>(
                    reader.String("DialogueEndPolicy")),
                Type1 = reader.Enum<InteractionConstants.Type>("Type1"),
                Value1 = reader.Int("Value1"),
                CustomTypeKey1 = reader.String("CustomTypeKey1"),
                Type2 = reader.Enum<InteractionConstants.Type>("Type2"),
                Value2 = reader.Int("Value2"),
                CustomTypeKey2 = reader.String("CustomTypeKey2"),
                Type3 = reader.Enum<InteractionConstants.Type>("Type3"),
                Value3 = reader.Int("Value3"),
                CustomTypeKey3 = reader.String("CustomTypeKey3"),
            };
        }
    }
}
