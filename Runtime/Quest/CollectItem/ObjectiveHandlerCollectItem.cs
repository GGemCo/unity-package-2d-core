namespace GGemCo2DCore
{
    /// <summary>
    /// 지정한 아이템 수집 목표를 처리합니다.
    /// </summary>
    public class ObjectiveHandlerCollectItem : ObjectiveHandlerBase
    {
        private QuestStep _currentStep;
        private int _currentCount;
        private QuestData _questData;
        private int _currentQuestUid;
        private bool _isRegisteredItemCollected;

        /// <summary>
        /// 기본 아이템 수집 목표 처리기를 생성합니다.
        /// </summary>
        public ObjectiveHandlerCollectItem()
        {
        }

        /// <summary>
        /// 목표 완료 요청을 전달할 소유자를 지정하여 아이템 수집 목표 처리기를 생성합니다.
        /// </summary>
        /// <param name="completionSink">목표 완료 요청을 받을 소유자입니다.</param>
        public ObjectiveHandlerCollectItem(IObjectiveCompletionSink completionSink) : base(completionSink)
        {
        }

        /// <summary>
        /// 아이템 수집 목표를 시작하고 아이템 획득 이벤트를 구독합니다.
        /// </summary>
        /// <param name="questUid">진행 중인 퀘스트 UID입니다.</param>
        /// <param name="step">현재 퀘스트 단계 정보입니다.</param>
        /// <param name="stepIndex">현재 퀘스트 단계 인덱스입니다.</param>
        /// <param name="npcUid">아이템 수집 목표에서는 사용하지 않는 NPC UID입니다.</param>
        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            _currentQuestUid = questUid;
            _currentStep = step;
            _questData = SceneGame.Instance?.saveDataManager?.Quest;
            _currentCount = _questData != null ? _questData.GetCount(_currentQuestUid) : 0;
            RegisterItemCollectedEvent();
        }

        /// <summary>
        /// 현재 수집 수량이 목표 수량에 도달했는지 확인합니다.
        /// </summary>
        /// <param name="step">확인할 퀘스트 단계 정보입니다.</param>
        /// <returns>목표 수량 이상 수집했으면 true입니다.</returns>
        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return step != null && _currentCount >= step.count;
        }

        /// <summary>
        /// 아이템 획득 이벤트를 중복 없이 구독합니다.
        /// </summary>
        private void RegisterItemCollectedEvent()
        {
            if (_isRegisteredItemCollected) return;

            GameEventManager.ItemCollectedEvent += OnItemCollected;
            _isRegisteredItemCollected = true;
        }

        /// <summary>
        /// 아이템 획득 이벤트를 받아 목표 아이템 수집 수량을 갱신합니다.
        /// </summary>
        /// <param name="e">획득한 아이템 정보입니다.</param>
        private void OnItemCollected(ItemCollectedEventData e)
        {
            if (_currentStep == null || _questData == null) return;
            if (e.ItemUid != _currentStep.targetUid) return;

            _currentCount += e.Count;
            _questData.SaveCount(_currentQuestUid, _currentCount);
            if (_currentCount < _currentStep.count) return;

            CompleteObjective();
        }

        /// <summary>
        /// 아이템 수집 목표를 완료하고 소유 퀘스트 매니저에 다음 단계 진행을 요청합니다.
        /// </summary>
        private void CompleteObjective()
        {
            int completedQuestUid = _currentQuestUid;
            OnDispose();
            CompleteObjectiveThroughOwner(completedQuestUid);
        }

        /// <summary>
        /// 구독한 아이템 획득 이벤트를 해제합니다.
        /// </summary>
        public override void OnDispose()
        {
            if (!_isRegisteredItemCollected) return;

            GameEventManager.ItemCollectedEvent -= OnItemCollected;
            _isRegisteredItemCollected = false;
        }
    }
}
