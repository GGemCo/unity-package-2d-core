namespace GGemCo2DCore
{
    public interface IObjectiveHandler
    {
        void StartObjective(int questUid, QuestStep step, int stepIndex, int npcUid);
        bool IsObjectiveComplete(QuestStep step);
        void OnDispose();
    }
}