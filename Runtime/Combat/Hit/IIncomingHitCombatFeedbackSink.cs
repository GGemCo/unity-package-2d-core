using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 피격 처리 결과를 공격자 측 시스템(BT/스킬 드라이버 등)에 다시 전달하기 위한 공용 피드백 인터페이스입니다.
    /// </summary>
    public interface IIncomingHitCombatFeedbackSink
    {
        /// <summary>
        /// 피격 대상에서 확정된 전투 결과를 공격자 쪽으로 전달합니다.
        /// </summary>
        void NotifyIncomingHitResolved(in IncomingHitCombatFeedback feedback);
    }

    /// <summary>
    /// 피격 처리 후 공격자에게 되돌려주는 전투 결과 정보입니다.
    /// </summary>
    public readonly struct IncomingHitCombatFeedback
    {
        public readonly GameObject Attacker;
        public readonly GameObject Target;
        public readonly int SkillUid;
        public readonly int AttackId;
        public readonly MonsterSkillCombatOutcome Outcome;
        public readonly float Time;

        public IncomingHitCombatFeedback(
            GameObject attacker,
            GameObject target,
            int skillUid,
            int attackId,
            MonsterSkillCombatOutcome outcome,
            float time)
        {
            Attacker = attacker;
            Target = target;
            SkillUid = skillUid;
            AttackId = attackId;
            Outcome = outcome;
            Time = time;
        }
    }
}
