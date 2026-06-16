using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="MetadataDamage"/>에 포함된 MP 보상 메타데이터를 Core MP 획득 규칙으로 변환합니다.
    /// </summary>
    /// <remarks>
    /// Skill, Projectile, Laser처럼 상위 패키지에서 발생한 타격 보상은 Core가 해당 패키지를 직접 참조하지 않도록
    /// <see cref="MetadataDamage.SkillHitMpGain"/>에 담아 전달합니다. 이 Provider는 메타데이터만 해석하므로
    /// 게임 전용 보상 수치나 조건은 알지 않습니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MetadataMpGainRuleProvider : MonoBehaviour, IMpGainRuleProvider
    {
        /// <summary>
        /// 타격 메타데이터에 명시된 MP 획득 보상을 반환합니다.
        /// </summary>
        /// <param name="context">Core MP 획득 컨텍스트입니다.</param>
        /// <param name="reward">메타데이터에서 해석한 MP 보상입니다.</param>
        /// <returns>지급 가능한 메타데이터 보상이 있으면 <see langword="true"/>입니다.</returns>
        public bool TryGetMpGainReward(in MpGainContext context, out MpGainReward reward)
        {
            reward = MpGainReward.None;
            if (context.Owner != gameObject ||
                context.Trigger != MpGainTrigger.OutgoingAttackHit ||
                context.AttackOutcome != MonsterSkillCombatOutcome.Hit)
            {
                return false;
            }

            MetadataDamage metadataDamage = context.MetadataDamage;
            if (metadataDamage == null || metadataDamage.SkillHitMpGain <= 0)
            {
                return false;
            }

            reward = MpGainReward.Create(
                MpGainRewardKind.SkillHitSuccess,
                metadataDamage.SkillHitMpGain,
                metadataDamage.AllowMultipleSkillHitMpGainPerAttack);
            return reward.IsValid;
        }
    }
}
