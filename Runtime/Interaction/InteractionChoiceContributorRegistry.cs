using System.Collections.Generic;
using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 외부 패키지의 NPC 인터랙션 선택지 제공자를 관리합니다.
    /// </summary>
    public static class InteractionChoiceContributorRegistry
    {
        private static readonly List<IInteractionChoiceContributor> Contributors =
            new List<IInteractionChoiceContributor>();

        /// <summary>
        /// 선택지 제공자를 등록합니다.
        /// </summary>
        /// <param name="contributor">등록할 선택지 제공자입니다.</param>
        public static void Register(IInteractionChoiceContributor contributor)
        {
            if (contributor == null || Contributors.Contains(contributor))
            {
                return;
            }

            Contributors.Add(contributor);
        }

        /// <summary>
        /// 선택지 제공자 등록을 해제합니다.
        /// </summary>
        /// <param name="contributor">등록 해제할 선택지 제공자입니다.</param>
        public static void Unregister(IInteractionChoiceContributor contributor)
        {
            if (contributor == null)
            {
                return;
            }

            Contributors.Remove(contributor);
        }

        /// <summary>
        /// 등록된 모든 제공자에게 현재 NPC의 선택지 구성을 요청합니다.
        /// </summary>
        /// <param name="npc">인터랙션 대상 캐릭터입니다.</param>
        /// <param name="npcData">대상 NPC의 테이블 데이터입니다.</param>
        /// <param name="interactionData">기본 인터랙션 테이블 데이터입니다.</param>
        /// <param name="results">선택지를 추가할 재사용 목록입니다.</param>
        public static void Collect(
            CharacterBase npc,
            StruckTableNpc npcData,
            StruckTableInteraction interactionData,
            List<InteractionChoiceContribution> results)
        {
            if (results == null)
            {
                return;
            }

            for (int i = 0; i < Contributors.Count; i++)
            {
                try
                {
                    Contributors[i]?.CollectChoices(npc, npcData, interactionData, results);
                }
                catch (Exception exception)
                {
                    GcLogger.LogException(exception);
                }
            }
        }
    }
}
