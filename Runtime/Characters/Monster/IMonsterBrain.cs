using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터의 전투/행동 의사결정(Decision) 루프를 담당하는 브레인(Brain) 인터페이스.
    /// </summary>
    /// <remarks>
    /// - 한 몬스터에는 동시에 여러 Brain이 붙을 수 있으나, 실제 틱은 우선순위가 가장 높은 활성 Brain 1개만 수행해야 한다.
    /// - Core는 특정 AI 구현(BT 등)을 모르며, 확장 패키지는 본 인터페이스를 구현하여 결합을 느슨하게 유지한다.
    /// </remarks>
    public interface IMonsterBrain
    {
        /// <summary>
        /// 우선순위. 값이 클수록 우선된다. (예: BT=100, Legacy=0)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Brain이 현재 동작 가능한 상태인지 여부.
        /// </summary>
        bool IsActive { get; }
        
        void OnCharacterTriggerEnter(Collider2D collision);
        void OnCharacterTriggerExit(Collider2D collision);
    }
}
