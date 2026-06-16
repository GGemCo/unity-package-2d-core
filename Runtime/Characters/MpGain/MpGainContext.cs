using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// MP 획득 규칙 Provider가 보상 여부를 판단할 때 사용하는 전투 피드백 컨텍스트입니다.
    /// </summary>
    public readonly struct MpGainContext
    {
        /// <summary>
        /// MP를 획득할 캐릭터 오브젝트입니다.
        /// </summary>
        public GameObject Owner { get; }

        /// <summary>
        /// 공격 또는 방어 상호작용의 상대 오브젝트입니다.
        /// </summary>
        public GameObject Target { get; }

        /// <summary>
        /// 타격 또는 가드 판정에 사용된 데미지 메타데이터입니다.
        /// </summary>
        public MetadataDamage MetadataDamage { get; }

        /// <summary>
        /// MP 획득 판정을 시작한 피드백 종류입니다.
        /// </summary>
        public MpGainTrigger Trigger { get; }

        /// <summary>
        /// 공격자 타격 결과입니다. 공격 피드백이 아닌 경우 기본값입니다.
        /// </summary>
        public MonsterSkillCombatOutcome AttackOutcome { get; }

        /// <summary>
        /// 가드 판정 결과입니다. 가드 피드백이 아닌 경우 기본값입니다.
        /// </summary>
        public GuardResolutionOutcome GuardOutcome { get; }

        /// <summary>
        /// 피드백이 확정된 Unity 시간입니다.
        /// </summary>
        public float Time { get; }

        /// <summary>
        /// MP 획득 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="owner">MP를 획득할 캐릭터 오브젝트입니다.</param>
        /// <param name="target">상대 오브젝트입니다.</param>
        /// <param name="metadataDamage">데미지 메타데이터입니다.</param>
        /// <param name="trigger">피드백 종류입니다.</param>
        /// <param name="attackOutcome">공격자 타격 결과입니다.</param>
        /// <param name="guardOutcome">가드 판정 결과입니다.</param>
        /// <param name="time">피드백 확정 시간입니다.</param>
        public MpGainContext(
            GameObject owner,
            GameObject target,
            MetadataDamage metadataDamage,
            MpGainTrigger trigger,
            MonsterSkillCombatOutcome attackOutcome,
            GuardResolutionOutcome guardOutcome,
            float time)
        {
            Owner = owner;
            Target = target;
            MetadataDamage = metadataDamage;
            Trigger = trigger;
            AttackOutcome = attackOutcome;
            GuardOutcome = guardOutcome;
            Time = time;
        }
    }
}
