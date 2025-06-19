namespace GGemCo.Scripts
{
    public class ObjectiveHandlerTalkToNpc : ObjectiveHandlerBase
    {
        private QuestStep currentStep;
        private int currentQuestUid;
        private int currentStepIndex = 0;
        
        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            if (step.targetUid != npcUid) return;
            if (step.dialogueUid <= 0) return;
            currentQuestUid = questUid;
            currentStep = step;
            currentStepIndex = stepIndex;

            // 시작하는 npc 업데이트
            Npc npc = SceneGame.Instance.mapManager.GetNpcByUid(npcUid) as Npc;
            npc?.UpdateQuestInfo();
            
            GameEventManager.OnDialogStart += OnDialogStart;
        }

        private void OnDialogStart(int npcUid)
        {
            UIWindowDialogue uiWindowDialogue =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowDialogue>(UIWindowManager.WindowUid
                    .Dialogue);
            uiWindowDialogue?.LoadDialogue(currentStep.dialogueUid, npcUid);
            GameEventManager.OnDialogStart -= OnDialogStart;
            
            GameEventManager.OnDialogEnd += OnDialogEnd;
        }

        private void OnDialogEnd(int npcUid)
        {
            if (currentStep.targetUid != npcUid) return;
            // 순서 중요
            GameEventManager.OnDialogEnd -= OnDialogEnd;
            SceneGame.Instance.QuestManager.NextStep(currentQuestUid);
            
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
            GameEventManager.OnDialogEnd -= OnDialogEnd;
        }
    }
}