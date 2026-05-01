using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 퀘스트 목표 타입에 맞는 에디터 단계 입력 UI를 제공합니다.
    /// </summary>
    public static class QuestStepDrawerFactory
    {
        private static readonly Dictionary<QuestConstants.ObjectiveType, IQuestStepDrawer> drawers =
            new Dictionary<QuestConstants.ObjectiveType, IQuestStepDrawer>
            {
                { QuestConstants.ObjectiveType.TalkToNpc, new StepDrawerTalkToNpc() },
                { QuestConstants.ObjectiveType.KillMonster, new StepDrawerKillMonster() },
                { QuestConstants.ObjectiveType.KillMonsterInMap, new StepDrawerKillMonsterInMap() },
                { QuestConstants.ObjectiveType.ReachPosition, new StepDrawerReachPosition() },
                { QuestConstants.ObjectiveType.CollectItem, new StepDrawerCollectItem() },
                { QuestConstants.ObjectiveType.EnterMap, new StepDrawerEnterMap() },
                // 나머지 ObjectiveType들도 여기에 추가
            };

        /// <summary>
        /// 목표 타입에 대응하는 단계 입력 UI 처리기를 반환합니다.
        /// </summary>
        /// <param name="type">확인할 퀘스트 목표 타입입니다.</param>
        /// <returns>목표 타입에 맞는 단계 입력 UI 처리기입니다. 등록되지 않은 타입이면 null을 반환합니다.</returns>
        public static IQuestStepDrawer GetDrawer(QuestConstants.ObjectiveType type)
        {
            return drawers.TryGetValue(type, out var drawer) ? drawer : null;
        }
    }
}
