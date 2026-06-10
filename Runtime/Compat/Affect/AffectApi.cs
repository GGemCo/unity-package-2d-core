using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Affect 패키지(옵션)를 Core/Skill 등 다른 패키지에서 안전하게 사용할 수 있도록 제공하는 Public Facade.
    /// - Core는 Affect 패키지를 직접 참조하지 않고 Reflection 브리지(<see cref="AffectRuntimeBridge"/>)로 연결합니다.
    /// - Skill 패키지 등에서는 이 Facade만 호출하도록 하여 패키지 의존성을 유지합니다.
    /// </summary>
    public static class AffectApi
    {
        /// <summary>
        /// Affect 런타임이 프로젝트에 설치되어 있는지 여부.
        /// </summary>
        public static bool HasRuntime()
        {
            return AffectRuntimeBridge.HasAffectRuntime();
        }

        /// <summary>
        /// 대상 GameObject에 Affect 실행에 필요한 컴포넌트들을 자동 부착합니다.
        /// (Affect 미설치 시 아무 일도 하지 않습니다.)
        /// </summary>
        public static void Ensure(GameObject target)
        {
            AffectRuntimeBridge.EnsureAffectSystem(target);
        }

        /// <summary>
        /// 대상에게 Affect를 적용합니다.
        /// - Affect 미설치 시 아무 일도 하지 않습니다.
        /// </summary>
        /// <param name="target">Affect를 적용할 대상입니다.</param>
        /// <param name="affectUid">적용할 Affect UID입니다.</param>
        /// <param name="source">Affect 발생 원천입니다.</param>
        /// <param name="durationOverrideSeconds">기본 지속시간을 대체할 초 단위 값입니다. 0 이하이면 Affect 기본값을 사용합니다.</param>
        /// <param name="durationBonusSeconds">기본 또는 오버라이드 지속시간에 추가할 초 단위 보너스입니다.</param>
        /// <param name="healHpBonus">힐 계열 Modifier가 실제 회복을 실행할 때 최종 회복량에 더할 HP 값입니다.</param>
        /// <param name="healHpMultiplier">힐 계열 Modifier가 실제 회복을 실행할 때 최종 회복량에 곱할 배율입니다.</param>
        public static void Apply(
            GameObject target,
            int affectUid,
            GameObject source = null,
            float durationOverrideSeconds = 0f,
            float durationBonusSeconds = 0f,
            long healHpBonus = 0L,
            float healHpMultiplier = 1f)
        {
            AffectRuntimeBridge.ApplyAffect(
                target,
                affectUid,
                source,
                durationOverrideSeconds,
                durationBonusSeconds,
                healHpBonus,
                healHpMultiplier);
        }

        /// <summary>
        /// 대상에서 특정 Affect를 제거합니다.
        /// </summary>
        public static void Remove(GameObject target, int affectUid)
        {
            AffectRuntimeBridge.RemoveAffect(target, affectUid);
        }

        /// <summary>
        /// 대상에서 모든 Affect를 제거합니다.
        /// </summary>
        public static void RemoveAll(GameObject target)
        {
            AffectRuntimeBridge.RemoveAll(target);
        }

        /// <summary>
        /// 공격자가 피격 대상에 성공했음을 Affect 런타임에 알립니다.
        /// (예: 공격자 버프의 OnHit 트리거 처리)
        /// </summary>
        public static void NotifyOnHit(GameObject attacker, GameObject hitTarget)
        {
            AffectRuntimeBridge.NotifyOnHit(attacker, hitTarget);
        }
        public static bool Has(GameObject target, int affectUid)
        {
            return AffectRuntimeBridge.HasAffect(target, affectUid);
        }

        /// <summary>
        /// 대상에 Affect 시스템이 이미 붙어 있을 때만 특정 Affect 보유 여부를 조회합니다.
        /// 조회 과정에서 컴포넌트를 자동 부착하지 않습니다.
        /// </summary>
        public static bool HasAttached(GameObject target, int affectUid)
        {
            return AffectRuntimeBridge.HasAttachedAffect(target, affectUid);
        }
    }
}
