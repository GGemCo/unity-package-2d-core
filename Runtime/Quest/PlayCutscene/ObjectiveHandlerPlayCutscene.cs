namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="QuestConstants.ObjectiveType.PlayCutscene"/> 목표를 처리합니다.
    /// 지정한 컷신이 정상 종료되면 해당 퀘스트를 다음 단계로 진행시킵니다.
    /// </summary>
    public sealed class ObjectiveHandlerPlayCutscene : ObjectiveHandlerBase
    {
        private int _currentQuestUid;
        private int _currentCutsceneUid;
        private bool _isRegisteredCutsceneCompleted;

        /// <summary>
        /// 컷신 목표를 시작하고 완료 이벤트를 구독합니다.
        /// </summary>
        /// <param name="questUid">진행 중인 퀘스트 UID입니다.</param>
        /// <param name="step">현재 퀘스트 단계 데이터입니다.</param>
        /// <param name="stepIndex">현재 단계 인덱스입니다.</param>
        /// <param name="npcUid">PlayCutscene 목표에서는 사용하지 않는 값입니다.</param>
        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            if (questUid <= 0 || step == null)
            {
                return;
            }

            SceneGame sceneGame = SceneGame.Instance;
            CutsceneManager cutsceneManager = sceneGame?.CutsceneManager;
            if (cutsceneManager == null)
            {
                GcLogger.LogError("[Quest] 컷신 매니저가 없어 PlayCutscene 목표를 시작할 수 없습니다.");
                return;
            }

            _currentQuestUid = questUid;
            _currentCutsceneUid = step.GetPlayCutsceneUid();
            if (_currentCutsceneUid <= 0)
            {
                GcLogger.LogError($"[Quest] PlayCutscene 목표에 유효한 cutsceneUid가 없습니다. questUid: {questUid}, stepIndex: {stepIndex}");
                return;
            }

            if (!_isRegisteredCutsceneCompleted)
            {
                cutsceneManager.CutsceneCompleted += OnCutsceneCompleted;
                _isRegisteredCutsceneCompleted = true;
            }

            if (!cutsceneManager.TryPlayCutscene(_currentCutsceneUid))
            {
                GcLogger.LogError($"[Quest] 컷신 재생에 실패했습니다. questUid: {questUid}, cutsceneUid: {_currentCutsceneUid}");
                OnDispose();
            }
        }

        /// <summary>
        /// PlayCutscene 목표는 이벤트 기반 완료이므로 직접 상태 판정은 사용하지 않습니다.
        /// </summary>
        /// <param name="step">현재 단계 정보입니다.</param>
        /// <returns>항상 <see langword="false"/>를 반환합니다.</returns>
        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return false;
        }

        /// <summary>
        /// 컷신 정상 종료 이벤트를 수신하면 현재 단계를 완료 처리합니다.
        /// </summary>
        /// <param name="cutsceneUid">정상 종료된 컷신 UID입니다.</param>
        private void OnCutsceneCompleted(int cutsceneUid)
        {
            if (_currentQuestUid <= 0) return;
            if (cutsceneUid != _currentCutsceneUid) return;

            int completedQuestUid = _currentQuestUid;
            OnDispose();
            SceneGame.Instance?.QuestManager?.NextStep(completedQuestUid);
        }

        /// <summary>
        /// 등록한 컷신 완료 이벤트를 해제합니다.
        /// </summary>
        public override void OnDispose()
        {
            if (_isRegisteredCutsceneCompleted && SceneGame.Instance?.CutsceneManager != null)
            {
                SceneGame.Instance.CutsceneManager.CutsceneCompleted -= OnCutsceneCompleted;
            }

            _isRegisteredCutsceneCompleted = false;
            _currentQuestUid = 0;
            _currentCutsceneUid = 0;
        }
    }
}
