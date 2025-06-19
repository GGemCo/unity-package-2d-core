using UnityEngine;

namespace GGemCo.Scripts
{
    public class ObjectiveHandlerFactory
    {
        public IObjectiveHandler CreateHandler(QuestConstants.ObjectiveType type)
        {
            switch (type)
            {
                case QuestConstants.ObjectiveType.TalkToNpc:
                    return new ObjectiveHandlerTalkToNpc();
                case QuestConstants.ObjectiveType.KillMonster:
                    return new ObjectiveHandlerKillMonster();
                case QuestConstants.ObjectiveType.CollectItem:
                    return new ObjectiveHandlerCollectItem();
                default:
                    Debug.LogWarning($"Unsupported ObjectiveType: {type}");
                    return null;
            }
        }
    }
}