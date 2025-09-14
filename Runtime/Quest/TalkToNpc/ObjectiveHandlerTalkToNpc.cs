namespace GGemCo2DCore
{
    public class ObjectiveHandlerTalkToNpc : ObjectiveHandlerBase
    {
        private QuestStep _currentStep;
        private int _currentQuestUid;
        private int _currentStepIndex = 0;
        private bool _isRegisteredDialogStart = false;
        private bool _isRegisteredDialogEnd = false;
        
        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            if (step.targetUid != npcUid) return;
            if (step.dialogueUid <= 0) return;
            _currentQuestUid = questUid;
            _currentStep = step;
            _currentStepIndex = stepIndex;

            // 시작하는 npc 업데이트
            Npc npc = SceneGame.Instance.mapManager.GetNpcByUid(npcUid) as Npc;
            npc?.UpdateQuestInfo();

            if (!_isRegisteredDialogStart)
            {
                GameEventManager.DialogStartEvent += OnDialogStart;
                _isRegisteredDialogStart = true;
            }
        }

        private void OnDialogStart(DialogEventData eventData)
        {
            int npcUid = eventData.NpcUid;
            UIWindowDialogue uiWindowDialogue =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowDialogue>(UIWindowConstants.WindowUid
                    .Dialogue);
            uiWindowDialogue?.LoadDialogue(_currentStep.dialogueUid, npcUid);
            
            if (!_isRegisteredDialogEnd)
            {
                GameEventManager.DialogEndEvent += OnDialogEnd;
                _isRegisteredDialogEnd = true;
            }
        }

        private void OnDialogEnd(DialogEventData eventData)
        {
            int npcUid = eventData.NpcUid;
            if (_currentStep.targetUid != npcUid) return;
            // 순서 중요
            GameEventManager.DialogStartEvent -= OnDialogStart;
            GameEventManager.DialogEndEvent -= OnDialogEnd;
            SceneGame.Instance.QuestManager.NextStep(_currentQuestUid);
            
            // 종료하는 npc 업데이트
            Npc npc = SceneGame.Instance.mapManager.GetNpcByUid(npcUid) as Npc;
            npc?.UpdateQuestInfo();
        }

        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return false;
        }
        public override void OnDispose()
        {
            GameEventManager.DialogStartEvent -= OnDialogStart;
            GameEventManager.DialogEndEvent -= OnDialogEnd;
        }
    }
}