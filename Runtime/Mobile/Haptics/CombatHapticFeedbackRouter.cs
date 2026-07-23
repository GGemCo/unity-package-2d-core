using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 확정된 전투 결과를 로컬 플레이어용 모바일 햅틱 이벤트로 변환합니다.
    /// </summary>
    internal static class CombatHapticFeedbackRouter
    {
        /// <summary>
        /// 플레이어의 최종 가드 판정 결과에 맞는 햅틱을 요청합니다.
        /// </summary>
        /// <param name="defender">공격을 방어한 캐릭터입니다.</param>
        /// <param name="result">최종 가드 판정 결과입니다.</param>
        public static void NotifyGuardResolved(
            CharacterBase defender,
            in GuardResolutionResult result)
        {
            if (defender == null || !defender.IsPlayer() || !result.IsResolved)
            {
                return;
            }

            switch (result.Outcome)
            {
                case GuardResolutionOutcome.Guarded:
                    MobileHapticService.TryPlay(CombatHapticEventType.GuardSuccess);
                    break;

                case GuardResolutionOutcome.JustGuarded:
                    MobileHapticService.TryPlay(CombatHapticEventType.JustGuardSuccess);
                    break;
            }
        }

        /// <summary>
        /// 플레이어가 몬스터에게 적용한 양수의 즉시 피해가 확정되면 피격 햅틱을 요청합니다.
        /// </summary>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <param name="metadataDamage">최종 피해량과 공격자 정보를 포함한 메타데이터입니다.</param>
        public static void NotifyMonsterHitConfirmed(
            CharacterBase target,
            MetadataDamage metadataDamage)
        {
            if (target == null ||
                !target.IsMonster() ||
                metadataDamage == null ||
                metadataDamage.damage <= 0L ||
                metadataDamage.IsDamageOverTime ||
                metadataDamage.attacker == null)
            {
                return;
            }

            CharacterBase attacker =
                metadataDamage.attacker.GetComponentInParent<CharacterBase>();
            if (attacker == null || !attacker.IsPlayer())
            {
                return;
            }

            MobileHapticService.TryPlay(CombatHapticEventType.MonsterHit);
        }
    }
}
