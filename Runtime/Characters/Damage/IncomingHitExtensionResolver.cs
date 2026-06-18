using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 피격 대상에 부착된 Core 확장 인터페이스를 검색하고 순서대로 호출합니다.
    /// </summary>
    internal static class IncomingHitExtensionResolver
    {
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
            if (target == null)
                return proposedHp;

            long resolvedHp = proposedHp;
            MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IIncomingHitFinalHpResolver resolver)
                    resolvedHp = resolver.ResolveFinalHpOnIncomingHit(resolvedHp, metadataDamage);
            }

            return resolvedHp;
        }
    }
}
