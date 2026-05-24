namespace GGemCo2DCore
{
    /// <summary>
    /// 지정한 NPC와의 대화 완료 목표를 처리합니다.
    /// </summary>
    public class ObjectiveHandlerTalkToNpc : ObjectiveHandlerBase
    {
        private QuestStep _currentStep;
        private int _currentQuestUid;
        private int _currentStepIndex;
        private bool _isRegisteredDialogStart;
        private bool _isRegisteredDialogEnd;

        /// <summary>
        /// 기본 NPC 대화 목표 처리기를 생성합니다.
        /// </summary>
        public ObjectiveHandlerTalkToNpc()
        {
        }

        /// <summary>
        /// 목표 완료 요청을 전달할 소유자를 지정하여 NPC 대화 목표 처리기를 생성합니다.
        /// </summary>
        /// <param name="completionSink">목표 완료 요청을 받을 소유자입니다.</param>
        public ObjectiveHandlerTalkToNpc(IObjectiveCompletionSink completionSink) : base(completionSink)
        {
        }

        /// <summary>
        /// NPC 대화 목표를 시작하고 대화 시작 이벤트를 구독합니다.
        /// </summary>
        /// <param name="questUid">진행 중인 퀘스트 UID입니다.</param>
        /// <param name="step">현재 퀘스트 단계 정보입니다.</param>
        /// <param name="stepIndex">현재 퀘스트 단계 인덱스입니다.</param>
        /// <param name="npcUid">대화 대상 NPC UID입니다.</param>
        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            if (step == null || step.targetUid != npcUid) return;
            if (step.dialogueUid <= 0) return;

            _currentQuestUid = questUid;
            _currentStep = step;
            _currentStepIndex = stepIndex;

            UpdateNpcQuestInfo(npcUid);
            RegisterDialogStartEvent();
        }

        /// <summary>
        /// NPC 대화 목표는 이벤트 기반 완료이므로 직접 상태 판정은 사용하지 않습니다.
        /// </summary>
        /// <param name="step">현재 단계 정보입니다.</param>
        /// <returns>항상 <see langword="false"/>를 반환합니다.</returns>
        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return false;
        }

        /// <summary>
        /// 대화 시작 이벤트를 중복 없이 구독합니다.
        /// </summary>
        private void RegisterDialogStartEvent()
        {
            if (_isRegisteredDialogStart) return;

            GameEventManager.DialogStartEvent += OnDialogStart;
            _isRegisteredDialogStart = true;
        }

        /// <summary>
        /// 대화 종료 이벤트를 중복 없이 구독합니다.
        /// </summary>
        private void RegisterDialogEndEvent()
        {
            if (_isRegisteredDialogEnd) return;

            GameEventManager.DialogEndEvent += OnDialogEnd;
            _isRegisteredDialogEnd = true;
        }

        /// <summary>
        /// 대화 시작 이벤트를 받아 대상 NPC의 대화 데이터를 로드합니다.
        /// </summary>
        /// <param name="eventData">대화를 시작한 NPC 정보입니다.</param>
        private void OnDialogStart(DialogEventData eventData)
        {
            if (_currentStep == null) return;

            int npcUid = eventData.NpcUid;
            UIWindowDialogue uiWindowDialogue =
                SceneGame.Instance?.uIWindowManager?.GetUIWindowByUid<UIWindowDialogue>(UIWindowConstants.WindowUid
                    .Dialogue);
            uiWindowDialogue?.LoadDialogue(_currentStep.dialogueUid, npcUid);

            RegisterDialogEndEvent();
        }

        /// <summary>
        /// 대화 종료 이벤트를 받아 대상 NPC와의 대화가 끝났으면 목표를 완료합니다.
        /// </summary>
        /// <param name="eventData">대화를 종료한 NPC 정보입니다.</param>
        private void OnDialogEnd(DialogEventData eventData)
        {
            if (_currentStep == null) return;

            int npcUid = eventData.NpcUid;
            if (_currentStep.targetUid != npcUid) return;

            int completedQuestUid = _currentQuestUid;
            OnDispose();
            CompleteObjectiveThroughOwner(completedQuestUid);
            UpdateNpcQuestInfo(npcUid);
        }

        /// <summary>
        /// NPC의 퀘스트 표시 정보를 갱신합니다.
        /// </summary>
        /// <param name="npcUid">갱신할 NPC UID입니다.</param>
        private void UpdateNpcQuestInfo(int npcUid)
        {
            Npc npc = SceneGame.Instance?.mapManager?.GetNpcByUid(npcUid) as Npc;
            npc?.UpdateQuestInfo();
        }

        /// <summary>
        /// 구독한 대화 이벤트를 해제합니다.
        /// </summary>
        public override void OnDispose()
        {
            if (_isRegisteredDialogStart)
            {
                GameEventManager.DialogStartEvent -= OnDialogStart;
                _isRegisteredDialogStart = false;
            }

            if (_isRegisteredDialogEnd)
            {
                GameEventManager.DialogEndEvent -= OnDialogEnd;
                _isRegisteredDialogEnd = false;
            }
        }
    }
}
