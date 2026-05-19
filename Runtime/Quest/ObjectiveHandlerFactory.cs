using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 퀘스트 목표 타입에 맞는 목표 처리기를 생성합니다.
    /// </summary>
    public class ObjectiveHandlerFactory
    {
        /// <summary>
        /// 목표 타입에 대응하는 목표 처리기 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="type">생성할 목표 처리기의 목표 타입입니다.</param>
        /// <returns>목표 타입에 맞는 처리기입니다. 지원하지 않는 타입이면 null을 반환합니다.</returns>
        public IObjectiveHandler CreateHandler(QuestConstants.ObjectiveType type)
        {
            switch (type)
            {
                case QuestConstants.ObjectiveType.TalkToNpc:
                    return new ObjectiveHandlerTalkToNpc();
                case QuestConstants.ObjectiveType.KillMonster:
                    return new ObjectiveHandlerKillMonster();
                case QuestConstants.ObjectiveType.KillMonsterInMap:
                    return new ObjectiveHandlerKillMonsterInMap();
                case QuestConstants.ObjectiveType.CollectItem:
                    return new ObjectiveHandlerCollectItem();
                case QuestConstants.ObjectiveType.EnterMap:
                    return new ObjectiveHandlerEnterMap();
                case QuestConstants.ObjectiveType.PlayCutscene:
                    return new ObjectiveHandlerPlayCutscene();
                default:
                    Debug.LogWarning($"Unsupported ObjectiveType: {type}");
                    return null;
            }
        }
    }
}
