namespace GGemCo2DCore
{
    /// <summary>
    /// 인터랙션 대화 그래프가 종료된 뒤의 후속 처리 정책입니다.
    /// </summary>
    public enum InteractionDialogueEndPolicy
    {
        /// <summary>
        /// 대화가 끝나면 기본 인터랙션/퀘스트 선택지를 이어서 표시합니다.
        /// </summary>
        ShowInteractionChoices = 0,

        /// <summary>
        /// 대화가 끝나면 인터랙션 창을 즉시 닫습니다.
        /// </summary>
        Close = 1,
    }
}
