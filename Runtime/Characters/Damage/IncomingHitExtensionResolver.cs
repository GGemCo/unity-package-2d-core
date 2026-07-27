using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 피격 대상에 부착된 Core 확장 인터페이스를 검색하고 순서대로 호출합니다.
    /// </summary>
    internal static class IncomingHitExtensionResolver
    {
        /// <summary>
        /// 방어 판정 이후의 피해를 HP 계층에 적용하기 전에 외부 자원으로 소비합니다.
        /// </summary>
        /// <param name="target">피해 소비 처리기를 검색할 캐릭터입니다.</param>
        /// <param name="metadataDamage">현재 피격 메타데이터입니다.</param>
        /// <param name="incomingDamage">HP 계층에 적용될 피해량입니다.</param>
        /// <param name="result">처리 성공 시 남은 피해와 후속 피격 반응 정책입니다.</param>
        /// <returns>등록된 처리기가 피해를 소비했으면 <see langword="true"/>입니다.</returns>
        public static bool TryConsumeIncomingDamage(
            CharacterBase target,
            MetadataDamage metadataDamage,
            long incomingDamage,
            out IncomingHitDamageConsumptionResult result)
        {
            result = default;
            if (target == null || incomingDamage <= 0L)
                return false;

            MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IIncomingHitDamageConsumptionResolver resolver)
                    continue;
                if (resolver.TryConsumeIncomingDamage(incomingDamage, metadataDamage, out result))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 피격 대상의 액션 취소 수신자에게 인터럽트 사유를 전달합니다.
        /// </summary>
        /// <param name="target">액션 취소 수신자를 검색할 캐릭터입니다.</param>
        /// <param name="reason">액션 취소 사유입니다.</param>
        public static void NotifyActionCancelers(CharacterBase target, IncomingHitCancelReason reason)
        {
            if (target == null)
                return;

            MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IIncomingHitActionCanceler canceler)
                    canceler.CancelActionsOnIncomingHit(reason);
            }
        }

        /// <summary>
        /// 피격 대상의 최종 HP 보정기를 컴포넌트 순서대로 적용합니다.
        /// </summary>
        /// <param name="target">최종 HP 보정기를 검색할 캐릭터입니다.</param>
        /// <param name="metadataDamage">현재 피격 메타데이터입니다.</param>
        /// <param name="proposedHp">Core 계산 기준 최종 HP입니다.</param>
        /// <returns>등록된 보정기가 모두 반영된 최종 HP입니다.</returns>
        public static long ResolveFinalHp(
            CharacterBase target,
            MetadataDamage metadataDamage,
            long proposedHp)
        {
            return ResolveFinalHp(
                target,
                metadataDamage,
                proposedHp,
                out _,
                out _);
        }

        /// <summary>
        /// 치명타 보호를 우선 검사한 뒤 피격 대상의 최종 HP 보정기를 컴포넌트 순서대로 적용합니다.
        /// </summary>
        /// <param name="target">확장 처리기를 검색할 캐릭터입니다.</param>
        /// <param name="metadataDamage">현재 피격 메타데이터입니다.</param>
        /// <param name="proposedHp">Core 계산 기준 최종 HP입니다.</param>
        /// <param name="wasLethalProtected">치명타 보호 처리기가 이번 피격을 소비했는지 여부입니다.</param>
        /// <param name="lethalProtectionResult">치명타 보호 후속 처리 정책입니다.</param>
        /// <returns>등록된 보호 또는 보정 정책이 반영된 최종 HP입니다.</returns>
        public static long ResolveFinalHp(
            CharacterBase target,
            MetadataDamage metadataDamage,
            long proposedHp,
            out bool wasLethalProtected,
            out IncomingHitLethalProtectionResult lethalProtectionResult)
        {
            wasLethalProtected = false;
            lethalProtectionResult = default;
            if (target == null)
                return proposedHp;

            MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
            if (proposedHp <= 0L)
            {
                // 차징 게이지나 보호막처럼 피격 자체를 소비하는 정책은
                // 보스 페이즈 같은 일반 최종 HP 보정보다 먼저 평가합니다.
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is not IIncomingHitLethalProtectionResolver resolver)
                        continue;
                    if (!resolver.TryResolveLethalIncomingHit(
                            proposedHp,
                            metadataDamage,
                            out lethalProtectionResult))
                        continue;

                    wasLethalProtected = true;
                    return lethalProtectionResult.ResolvedHp;
                }
            }

            long resolvedHp = proposedHp;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IIncomingHitFinalHpResolver resolver)
                    resolvedHp = resolver.ResolveFinalHpOnIncomingHit(resolvedHp, metadataDamage);
            }

            return resolvedHp;
        }
    }
}
