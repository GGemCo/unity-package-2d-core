using UnityEngine;

namespace GGemCo.Scripts
{
    public class ObjectiveHandlerCollectItem : ObjectiveHandlerBase
    {
        private QuestStep currentStep;
        private int currentCount = 0;
        private QuestData questData;
        private int currentQuestUid;

        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            currentQuestUid = questUid;
            currentStep = step;
            questData = SceneGame.Instance.saveDataManager.Quest;
            currentCount = questData.GetCount(currentQuestUid);
            GameEventManager.OnItemCollected += OnItemCollected;
        }

        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return currentCount >= step.count;
        }
        private void OnItemCollected(int itemUid, int count)
        {
            if (itemUid == currentStep.targetUid)
            {
                currentCount += count;
                // GcLogger.Log($"[CollectObjective] {itemUid} 수집: {currentCount}/{currentStep.count}");
                questData.SaveCount(currentQuestUid, currentCount);
                if (currentCount >= currentStep.count)
                {
                    SceneGame.Instance.QuestManager.NextStep(currentQuestUid);
                    GameEventManager.OnItemCollected -= OnItemCollected;
                }
            }
        }

        public override void OnDispose()
        {
            GameEventManager.OnItemCollected -= OnItemCollected;
        }
    }
}