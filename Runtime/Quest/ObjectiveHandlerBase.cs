namespace GGemCo2DCore
{
    /// <summary>
    /// 퀘스트 목표 처리기의 공통 기반 클래스입니다.
    /// </summary>
    public abstract class ObjectiveHandlerBase : IObjectiveHandler
    {
        private readonly IObjectiveCompletionSink _completionSink;

        /// <summary>
        /// 기본 목표 처리기를 생성합니다.
        /// </summary>
        protected ObjectiveHandlerBase()
        {
        }

        /// <summary>
        /// 목표 완료 요청을 전달할 소유자를 지정하여 목표 처리기를 생성합니다.
        /// </summary>
        /// <param name="completionSink">목표 완료 요청을 받을 소유자입니다.</param>
        protected ObjectiveHandlerBase(IObjectiveCompletionSink completionSink)
        {
            _completionSink = completionSink;
        }

        /// <summary>
        /// 목표를 시작합니다.
        /// </summary>
        /// <param name="questUid">진행 중인 퀘스트 UID입니다.</param>
        /// <param name="step">시작할 퀘스트 단계 정보입니다.</param>
        /// <param name="stepIndex">시작할 퀘스트 단계 인덱스입니다.</param>
        /// <param name="npcUid">목표 시작에 사용할 NPC UID입니다.</param>
        public void StartObjective(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            StartObjectiveTyped(questUid, step, stepIndex, npcUid);
        }

        /// <summary>
        /// 목표 완료 여부를 확인합니다.
        /// </summary>
        /// <param name="step">확인할 퀘스트 단계 정보입니다.</param>
        /// <returns>목표가 완료되었으면 true입니다.</returns>
        public bool IsObjectiveComplete(QuestStep step)
        {
            return IsObjectiveCompleteTyped(step);
        }

        /// <summary>
        /// 목표 처리기가 구독한 이벤트나 임시 상태를 정리합니다.
        /// </summary>
        public virtual void OnDispose() { }

        /// <summary>
        /// 소유 퀘스트 매니저에 현재 목표 완료를 요청합니다.
        /// </summary>
        /// <param name="questUid">완료할 퀘스트 UID입니다.</param>
        protected void CompleteObjectiveThroughOwner(int questUid)
        {
            if (questUid <= 0) return;

            if (_completionSink != null)
            {
                _completionSink.CompleteObjective(questUid);
                return;
            }

            SceneGame.Instance?.QuestManager?.CompleteObjective(questUid);
        }

        /// <summary>
        /// 파생 클래스에서 실제 목표 시작 로직을 처리합니다.
        /// </summary>
        /// <param name="questUid">진행 중인 퀘스트 UID입니다.</param>
        /// <param name="step">시작할 퀘스트 단계 정보입니다.</param>
        /// <param name="stepIndex">시작할 퀘스트 단계 인덱스입니다.</param>
        /// <param name="npcUid">목표 시작에 사용할 NPC UID입니다.</param>
        protected abstract void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid);

        /// <summary>
        /// 파생 클래스에서 실제 목표 완료 판정 로직을 처리합니다.
        /// </summary>
        /// <param name="step">확인할 퀘스트 단계 정보입니다.</param>
        /// <returns>목표가 완료되었으면 true입니다.</returns>
        protected abstract bool IsObjectiveCompleteTyped(QuestStep step);
    }
}
