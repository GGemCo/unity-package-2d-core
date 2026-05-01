namespace GGemCo2DCore
{
    /// <summary>
    /// 지정한 맵에 입장하면 완료되는 퀘스트 목표를 처리합니다.
    /// </summary>
    public class ObjectiveHandlerEnterMap : ObjectiveHandlerBase
    {
        private QuestStep _currentStep;
        private int _currentQuestUid;
        private bool _isRegisteredMapEntered;

        /// <summary>
        /// EnterMap 목표를 시작하고 현재 맵 또는 이후 입장 이벤트가 목표 맵과 일치하는지 감시합니다.
        /// </summary>
        /// <param name="questUid">진행 중인 퀘스트 UID입니다.</param>
        /// <param name="step">현재 퀘스트 단계 정보입니다.</param>
        /// <param name="stepIndex">현재 퀘스트 단계 인덱스입니다.</param>
        /// <param name="npcUid">EnterMap 목표에서는 사용하지 않는 NPC UID입니다.</param>
        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            if (step == null || step.mapUid <= 0) return;

            _currentQuestUid = questUid;
            _currentStep = step;

            if (IsCurrentMapTarget())
            {
                CompleteObjective();
                return;
            }

            RegisterMapEnteredEvent();
        }

        /// <summary>
        /// 현재 로드된 맵이 목표 맵인지 확인합니다.
        /// </summary>
        /// <param name="step">확인할 퀘스트 단계 정보입니다.</param>
        /// <returns>현재 맵이 목표 맵이면 true입니다.</returns>
        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            if (step == null || step.mapUid <= 0) return false;

            MapManager mapManager = SceneGame.Instance?.mapManager;
            return mapManager != null && mapManager.GetCurrentMapUid() == step.mapUid;
        }

        /// <summary>
        /// 맵 입장 이벤트를 중복 없이 구독합니다.
        /// </summary>
        private void RegisterMapEnteredEvent()
        {
            if (_isRegisteredMapEntered) return;

            GameEventManager.MapEnteredEvent += OnMapEntered;
            _isRegisteredMapEntered = true;
        }

        /// <summary>
        /// 맵 입장 이벤트를 받아 목표 맵과 일치할 때 현재 단계를 완료합니다.
        /// </summary>
        /// <param name="eventData">입장 완료된 맵 정보입니다.</param>
        private void OnMapEntered(MapEnteredEventData eventData)
        {
            if (_currentStep == null || eventData.MapUid != _currentStep.mapUid) return;

            CompleteObjective();
        }

        /// <summary>
        /// 현재 로드된 맵이 진행 중인 목표 맵인지 확인합니다.
        /// </summary>
        /// <returns>현재 맵이 목표 맵이면 true입니다.</returns>
        private bool IsCurrentMapTarget()
        {
            return IsObjectiveCompleteTyped(_currentStep);
        }

        /// <summary>
        /// EnterMap 목표를 완료하고 다음 퀘스트 단계로 진행합니다.
        /// </summary>
        private void CompleteObjective()
        {
            if (_currentQuestUid <= 0) return;

            OnDispose();
            SceneGame.Instance?.QuestManager?.NextStep(_currentQuestUid);
        }

        /// <summary>
        /// 구독한 맵 입장 이벤트를 해제합니다.
        /// </summary>
        public override void OnDispose()
        {
            if (!_isRegisteredMapEntered) return;

            GameEventManager.MapEnteredEvent -= OnMapEntered;
            _isRegisteredMapEntered = false;
        }
    }
}
