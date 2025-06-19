using System.Collections.Generic;

namespace GGemCo.Scripts
{
    [System.Serializable]
    public class Quest
    {
        public int uid;
        public string title;
        public List<QuestStep> steps = new List<QuestStep>();
        public QuestReward reward = new QuestReward();
    }

    [System.Serializable]
    public class QuestStep
    {
        public QuestConstants.ObjectiveType objectiveType;
        
        public int mapUid;
        public int targetUid;
        public Vec2 position;
        public int count;
        public int dialogueUid;
    }
}