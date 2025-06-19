namespace GGemCo.Scripts
{
    public abstract class ObjectiveHandlerBase : IObjectiveHandler
    {
        public void StartObjective(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            StartObjectiveTyped(questUid, step, stepIndex, npcUid);
        }

        public bool IsObjectiveComplete(QuestStep step)
        {
            return IsObjectiveCompleteTyped(step);
        }

        public virtual void OnDispose() { }

        protected abstract void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid);
        protected abstract bool IsObjectiveCompleteTyped(QuestStep step);
    }
}