namespace GGemCo2DCore
{
    public class ObjectiveHandlerCollectItem : ObjectiveHandlerBase
    {
        private QuestStep _currentStep;
        private int _currentCount = 0;
        private QuestData _questData;
        private int _currentQuestUid;
        private bool _isRegisteredItemCollected = false;

        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            _currentQuestUid = questUid;
            _currentStep = step;
            _questData = SceneGame.Instance.saveDataManager.Quest;
            _currentCount = _questData.GetCount(_currentQuestUid);
            if (!_isRegisteredItemCollected)
            {
                GameEventManager.OnItemCollected += OnItemCollected;
                _isRegisteredItemCollected = true;
            }
        }

        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return _currentCount >= step.count;
        }
        private void OnItemCollected(int itemUid, int count)
        {
            if (itemUid == _currentStep.targetUid)
            {
                _currentCount += count;
                // GcLogger.Log($"[CollectObjective] {itemUid} 수집: {currentCount}/{currentStep.count}");
                _questData.SaveCount(_currentQuestUid, _currentCount);
                if (_currentCount >= _currentStep.count)
                {
                    SceneGame.Instance.QuestManager.NextStep(_currentQuestUid);
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