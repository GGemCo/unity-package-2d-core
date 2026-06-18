using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 확정된 피격 결과를 공격자 측 전투 피드백 수신자에게 전달합니다.
    /// </summary>
    internal static class CombatHitFeedbackNotifier
    {
        /// <summary>
        /// 스킬 피격 결과를 공격자의 수신자에게 전달합니다.
        /// </summary>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <param name="metadataDamage">처리된 데미지 메타데이터입니다.</param>
        /// <param name="outcome">최종 전투 결과입니다.</param>
        public static void NotifyIncoming(
            CharacterBase target,
            MetadataDamage metadataDamage,
            MonsterSkillCombatOutcome outcome)
        {
            if (metadataDamage == null || metadataDamage.attacker == null || metadataDamage.SkillUid <= 0)
                return;

            GameObject attacker = metadataDamage.attacker;
            var feedback = new IncomingHitCombatFeedback(
                attacker,
                target != null ? target.gameObject : null,
                metadataDamage.SkillUid,
                metadataDamage.AttackId,
                outcome,
                Time.time);

            MonoBehaviour[] behaviours = attacker.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IIncomingHitCombatFeedbackSink sink)
                    sink.NotifyIncomingHitResolved(in feedback);
            }
        }

        /// <summary>
        /// 실제 타격 결과와 메타데이터를 공격자의 수신자에게 전달합니다.
        /// </summary>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <param name="metadataDamage">처리된 데미지 메타데이터입니다.</param>
        /// <param name="outcome">최종 전투 결과입니다.</param>
        public static void NotifyOutgoing(
            CharacterBase target,
            MetadataDamage metadataDamage,
            MonsterSkillCombatOutcome outcome)
        {
            if (metadataDamage == null || metadataDamage.attacker == null)
                return;

            GameObject attacker = metadataDamage.attacker;
            var feedback = new OutgoingAttackHitFeedback(
                attacker,
                target != null ? target.gameObject : null,
                metadataDamage,
                outcome,
                Time.time);

            MonoBehaviour[] behaviours = attacker.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IOutgoingAttackHitFeedbackSink sink)
                    sink.NotifyOutgoingAttackHitResolved(in feedback);
            }
        }
    }
}
