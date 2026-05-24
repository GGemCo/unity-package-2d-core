namespace GGemCo2DCore
{
    /// <summary>
    /// 특정 위치 도달 목표를 처리하기 위한 기본 처리기입니다.
    /// </summary>
    public class ObjectiveHandlerReachPosition : ObjectiveHandlerBase
    {
        /// <summary>
        /// 기본 위치 도달 목표 처리기를 생성합니다.
        /// </summary>
        public ObjectiveHandlerReachPosition()
        {
        }

        /// <summary>
        /// 목표 완료 요청을 전달할 소유자를 지정하여 위치 도달 목표 처리기를 생성합니다.
        /// </summary>
        /// <param name="completionSink">목표 완료 요청을 받을 소유자입니다.</param>
        public ObjectiveHandlerReachPosition(IObjectiveCompletionSink completionSink) : base(completionSink)
        {
        }

        /// <summary>
        /// 위치 도달 목표를 시작합니다.
        /// </summary>
        /// <param name="questUid">진행 중인 퀘스트 UID입니다.</param>
        /// <param name="step">현재 퀘스트 단계 정보입니다.</param>
        /// <param name="stepIndex">현재 퀘스트 단계 인덱스입니다.</param>
        /// <param name="npcUid">위치 도달 목표에서는 사용하지 않는 NPC UID입니다.</param>
        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
        }

        /// <summary>
        /// 위치 도달 목표 완료 여부를 확인합니다.
        /// </summary>
        /// <param name="step">확인할 퀘스트 단계 정보입니다.</param>
        /// <returns>현재 구현에서는 항상 false입니다.</returns>
        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return false;
        }
    }
}
