using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 공격자가 실제 타격 확정 결과를 수신하기 위한 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 데미지를 받는 대상의 Core 처리 결과를 공격자 오브젝트에 되돌려주기 위한 포트입니다.
    /// 상위 패키지는 이 인터페이스를 구현해 기본 공격 콤보, 스킬 콤보, 전투 피드백을 연결할 수 있습니다.
    /// </remarks>
    public interface IOutgoingAttackHitFeedbackSink
    {
        /// <summary>
        /// 공격자의 타격 결과가 확정되었을 때 호출됩니다.
        /// </summary>
        /// <param name="feedback">타격 확정 결과입니다.</param>
        void NotifyOutgoingAttackHitResolved(in OutgoingAttackHitFeedback feedback);
    }

    /// <summary>
    /// 공격자에게 전달할 타격 확정 결과입니다.
    /// </summary>
    public readonly struct OutgoingAttackHitFeedback
    {
        /// <summary>
        /// 공격자 오브젝트입니다.
        /// </summary>
        public GameObject Attacker { get; }

        /// <summary>
        /// 피격 대상 오브젝트입니다.
        /// </summary>
        public GameObject Target { get; }

        /// <summary>
        /// 타격에 사용된 데미지 메타데이터입니다.
        /// </summary>
        public MetadataDamage MetadataDamage { get; }

        /// <summary>
        /// 최종 전투 결과입니다.
        /// </summary>
        public MonsterSkillCombatOutcome Outcome { get; }

        /// <summary>
        /// 결과가 확정된 Unity 시간입니다.
        /// </summary>
        public float Time { get; }

        /// <summary>
        /// 공격자 타격 결과 값을 생성합니다.
        /// </summary>
        /// <param name="attacker">공격자 오브젝트입니다.</param>
        /// <param name="target">피격 대상 오브젝트입니다.</param>
        /// <param name="metadataDamage">타격 메타데이터입니다.</param>
        /// <param name="outcome">최종 전투 결과입니다.</param>
        /// <param name="time">결과가 확정된 Unity 시간입니다.</param>
        public OutgoingAttackHitFeedback(
            GameObject attacker,
            GameObject target,
            MetadataDamage metadataDamage,
            MonsterSkillCombatOutcome outcome,
            float time)
        {
            Attacker = attacker;
            Target = target;
            MetadataDamage = metadataDamage;
            Outcome = outcome;
            Time = time;
        }
    }
}
