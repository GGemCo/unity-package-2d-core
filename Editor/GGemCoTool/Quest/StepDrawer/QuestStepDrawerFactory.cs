using System.Collections.Generic;
using GGemCo.Editor;
using GGemCo.Scripts;

public static class QuestStepDrawerFactory
{
    private static readonly Dictionary<QuestConstants.ObjectiveType, IQuestStepDrawer> drawers =
        new Dictionary<QuestConstants.ObjectiveType, IQuestStepDrawer>
        {
            { QuestConstants.ObjectiveType.TalkToNpc, new StepDrawerTalkToNpc() },
            { QuestConstants.ObjectiveType.KillMonster, new StepDrawerKillMonster() },
            { QuestConstants.ObjectiveType.ReachPosition, new StepDrawerReachPosition() },
            { QuestConstants.ObjectiveType.CollectItem, new StepDrawerCollectItem() },
            // 나머지 ObjectiveType들도 여기에 추가
        };

    public static IQuestStepDrawer GetDrawer(QuestConstants.ObjectiveType type)
    {
        return drawers.TryGetValue(type, out var drawer) ? drawer : null;
    }
}