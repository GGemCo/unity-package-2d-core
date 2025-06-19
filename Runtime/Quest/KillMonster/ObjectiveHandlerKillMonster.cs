using UnityEngine;

namespace GGemCo.Scripts
{
    public class ObjectiveHandlerKillMonster : ObjectiveHandlerBase
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
            GameEventManager.OnMonsterKilled += OnMonsterKilled;
        }
        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return currentCount >= step.count;
        }
        private void OnMonsterKilled(int mapUid, int monsterUid)
        {
            if (mapUid != currentStep.mapUid) return;
            if (monsterUid != currentStep.targetUid) return;
            currentCount++;
            // GcLogger.Log($"[KillObjective] {monsterUid} 처치: {currentCount}/{currentStep.count}");
            questData.SaveCount(currentQuestUid, currentCount);
            if (currentCount >= currentStep.count)
            {
                SceneGame.Instance.QuestManager.NextStep(currentQuestUid);
                GameEventManager.OnMonsterKilled -= OnMonsterKilled;
            }
        }

        public override void OnDispose()
        {
            GameEventManager.OnMonsterKilled -= OnMonsterKilled;
        }
    }
}