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
        /// <param name="completionSink">목표 완료 요청을 받을 소유 퀘스트 매니저입니다.</param>
        /// <returns>목표 타입에 맞는 처리기입니다. 지원하지 않는 타입이면 null을 반환합니다.</returns>
        public IObjectiveHandler CreateHandler(QuestConstants.ObjectiveType type,
            IObjectiveCompletionSink completionSink = null)
        {
            switch (type)
            {
                case QuestConstants.ObjectiveType.TalkToNpc:
                    return new ObjectiveHandlerTalkToNpc(completionSink);
                case QuestConstants.ObjectiveType.KillMonster:
                    return new ObjectiveHandlerKillMonster(completionSink);
                case QuestConstants.ObjectiveType.KillMonsterInMap:
                    return new ObjectiveHandlerKillMonsterInMap(completionSink);
                case QuestConstants.ObjectiveType.CollectItem:
                    return new ObjectiveHandlerCollectItem(completionSink);
                case QuestConstants.ObjectiveType.EnterMap:
                    return new ObjectiveHandlerEnterMap(completionSink);
                case QuestConstants.ObjectiveType.PlayCutscene:
                    return new ObjectiveHandlerPlayCutscene(completionSink);
                default:
                    Debug.LogWarning($"Unsupported ObjectiveType: {type}");
                    return null;
            }
        }
    }
}
