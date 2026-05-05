using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 이번 인터랙션 세션에서 실제로 재생할 dialogue 선택 결과입니다.
    /// </summary>
    [Serializable]
    public readonly struct InteractionDialogueSelectionResult
    {
        /// <summary>
        /// 비어 있는 선택 결과입니다.
        /// </summary>
        public static readonly InteractionDialogueSelectionResult None = new InteractionDialogueSelectionResult(0, string.Empty);

        /// <summary>
        /// 실제로 재생할 dialogue UID 입니다.
        /// </summary>
        public int DialogueUid { get; }

        /// <summary>
        /// dialogue 진입 시 사용할 시작 노드 GUID 입니다.
        /// </summary>
        public string StartNodeGuid { get; }

        /// <summary>
        /// 유효한 dialogue 가 선택되었는지 여부입니다.
        /// </summary>
        public bool HasDialogue => DialogueUid > 0;

        /// <summary>
        /// dialogue 선택 결과를 생성합니다.
        /// </summary>
        /// <param name="dialogueUid">재생할 dialogue UID 입니다.</param>
        /// <param name="startNodeGuid">시작 노드 GUID 입니다.</param>
        public InteractionDialogueSelectionResult(int dialogueUid, string startNodeGuid)
        {
            DialogueUid = dialogueUid;
            StartNodeGuid = startNodeGuid ?? string.Empty;
        }
    }
}
