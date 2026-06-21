using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GGemCo2DCore
{
    /// <summary>
    /// 외부 패키지가 NPC 인터랙션 선택지를 제공할 때 구현하는 확장 포트입니다.
    /// </summary>
    public interface IInteractionChoiceContributor
    {
        /// <summary>
        /// 현재 NPC와 인터랙션 데이터에 맞는 선택지를 결과 목록에 추가합니다.
        /// </summary>
        /// <param name="npc">인터랙션 대상 캐릭터입니다.</param>
        /// <param name="npcData">대상 NPC의 테이블 데이터입니다.</param>
        /// <param name="interactionData">기본 인터랙션 테이블 데이터입니다.</param>
        /// <param name="results">외부 선택지를 추가할 결과 목록입니다.</param>
        void CollectChoices(
            CharacterBase npc,
            StruckTableNpc npcData,
            StruckTableInteraction interactionData,
            List<InteractionChoiceContribution> results);
    }

    /// <summary>
    /// 외부 패키지가 인터랙션 대화창에 전달하는 선택지 정보입니다.
    /// </summary>
    public sealed class InteractionChoiceContribution
    {
        /// <summary>
        /// 선택지에 표시할 문자열입니다.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// 선택 시 실행할 비동기 동작입니다.
        /// </summary>
        public Func<Task> ExecuteAsync { get; }

        /// <summary>
        /// 외부 인터랙션 선택지 정보를 생성합니다.
        /// </summary>
        /// <param name="label">선택지 표시 문자열입니다.</param>
        /// <param name="executeAsync">선택 시 실행할 비동기 동작입니다.</param>
        public InteractionChoiceContribution(string label, Func<Task> executeAsync)
        {
            Label = label ?? string.Empty;
            ExecuteAsync = executeAsync;
        }
    }
}
