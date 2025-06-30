namespace GGemCo2DCore
{
    public class ObjectiveHandlerKillMonster : ObjectiveHandlerBase
    {
        private QuestStep _currentStep;
        private int _currentCount = 0;
        private QuestData _questData;
        private int _currentQuestUid;
        private bool _isRegisteredMonsterKilled = false;

        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            _currentQuestUid = questUid;
            _currentStep = step;
            _questData = SceneGame.Instance.saveDataManager.Quest;
            _currentCount = _questData.GetCount(_currentQuestUid);
            if (!_isRegisteredMonsterKilled)
            {
                GameEventManager.OnMonsterKilled += OnMonsterKilled;
                _isRegisteredMonsterKilled = true;
            }
        }
        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return _currentCount >= step.count;
        }
        private void OnMonsterKilled(int mapUid, int monsterUid)
        {
            if (mapUid != _currentStep.mapUid) return;
            if (monsterUid != _currentStep.targetUid) return;
            _currentCount++;
            // GcLogger.Log($"[KillObjective] {monsterUid} 처치: {currentCount}/{currentStep.count}");
            _questData.SaveCount(_currentQuestUid, _currentCount);
            if (_currentCount >= _currentStep.count)
            {
                SceneGame.Instance.QuestManager.NextStep(_currentQuestUid);
                GameEventManager.OnMonsterKilled -= OnMonsterKilled;
            }
        }

        public override void OnDispose()
        {
            GameEventManager.OnMonsterKilled -= OnMonsterKilled;
        }
    }
}