using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 Threat 항목에 기여한 원인을 비트 플래그로 표현합니다.
    /// </summary>
    [Flags]
    public enum MonsterThreatSource
    {
        /// <summary>등록된 원인이 없습니다.</summary>
        None = 0,

        /// <summary>몬스터 중심 감지 범위에서 발견된 대상입니다.</summary>
        DetectionRange = 1 << 0,

        /// <summary>맵의 패트롤 또는 Encounter 영역에서 발견된 대상입니다.</summary>
        Patrol = 1 << 1,

        /// <summary>몬스터에게 확정 피해를 준 대상입니다.</summary>
        Damage = 1 << 2,

        /// <summary>도발, 스크립트, 보스 패턴 등 외부 시스템에서 추가한 Threat입니다.</summary>
        External = 1 << 3,
    }

    /// <summary>
    /// 몬스터의 Threat 누적과 현재 타겟 전환 정책을 런타임용으로 정규화한 불변 프로필입니다.
    /// </summary>
    /// <remarks>
    /// 범위 값은 <see cref="MonsterCombatRangeProfile"/>이 담당하고,
    /// 이 프로필은 대상별 Threat 점수와 타겟 전환 안정화 정책만 담당합니다.
    /// </remarks>
    public readonly struct MonsterThreatProfile
    {
        private const float DefaultDetectionThreat = 1f;
        private const float DefaultPatrolThreat = 1f;
        private const float DefaultDamageThreatMultiplier = 1f;
        private const float DefaultMinimumDamageThreat = 1f;
        private const float DefaultTargetSwitchThreatRatio = 1.1f;
        private const int DefaultMaxThreatTargets = 16;
        private const int MaximumThreatTargets = 64;

        /// <summary>감지 범위 진입 시 유지할 기본 Threat입니다.</summary>
        public float DetectionThreat { get; }

        /// <summary>패트롤 또는 Encounter 영역 진입 시 유지할 기본 Threat입니다.</summary>
        public float PatrolThreat { get; }

        /// <summary>확정 피해량을 Threat로 변환할 때 적용하는 배율입니다.</summary>
        public float DamageThreatMultiplier { get; }

        /// <summary>피해량이 작더라도 보장할 최소 피해 Threat입니다.</summary>
        public float MinimumDamageThreat { get; }

        /// <summary>
        /// 현재 타겟을 다른 대상으로 전환하기 위해 새 후보가 넘어야 하는 Threat 비율입니다.
        /// </summary>
        /// <remarks>
        /// 1이면 더 높은 Threat가 발생하는 즉시 전환하며, 1.1이면 현재 타겟보다 10% 이상 높아야 전환합니다.
        /// </remarks>
        public float TargetSwitchThreatRatio { get; }

        /// <summary>몬스터 한 개체가 동시에 기억할 수 있는 최대 Threat 대상 수입니다.</summary>
        public int MaxThreatTargets { get; }

        /// <summary>monster_combat_profile 테이블 행을 명시적으로 적용했는지 여부입니다.</summary>
        public bool IsConfigured { get; }

        private MonsterThreatProfile(
            bool isConfigured,
            float detectionThreat,
            float patrolThreat,
            float damageThreatMultiplier,
            float minimumDamageThreat,
            float targetSwitchThreatRatio,
            int maxThreatTargets)
        {
            IsConfigured = isConfigured;
            DetectionThreat = detectionThreat;
            PatrolThreat = patrolThreat;
            DamageThreatMultiplier = damageThreatMultiplier;
            MinimumDamageThreat = minimumDamageThreat;
            TargetSwitchThreatRatio = targetSwitchThreatRatio;
            MaxThreatTargets = maxThreatTargets;
        }

        /// <summary>
        /// monster_combat_profile 테이블 데이터에서 Threat 프로필을 생성합니다.
        /// </summary>
        /// <param name="tableData">선택한 몬스터 전투 프로필 테이블 행입니다.</param>
        /// <returns>기존 데이터와 신규 Threat 컬럼을 모두 지원하는 정규화된 프로필입니다.</returns>
        public static MonsterThreatProfile Create(StruckTableMonsterCombatProfile tableData)
        {
            return new MonsterThreatProfile(
                tableData != null,
                ResolvePositive(tableData?.DetectionThreat ?? 0f, DefaultDetectionThreat),
                ResolvePositive(tableData?.PatrolThreat ?? 0f, DefaultPatrolThreat),
                ResolvePositive(tableData?.DamageThreatMultiplier ?? 0f, DefaultDamageThreatMultiplier),
                ResolvePositive(tableData?.MinimumDamageThreat ?? 0f, DefaultMinimumDamageThreat),
                ResolveThreatSwitchRatio(tableData?.TargetSwitchThreatRatio ?? 0f),
                ResolveMaxThreatTargets(tableData?.MaxThreatTargets ?? 0));
        }

        /// <summary>
        /// 확정 피해량을 현재 프로필 기준 Threat로 변환합니다.
        /// </summary>
        /// <param name="confirmedDamage">방어력과 면역 판정 이후 확정된 피해량입니다.</param>
        /// <returns>대상에게 누적할 0보다 큰 Threat 값입니다.</returns>
        public float CalculateDamageThreat(long confirmedDamage)
        {
            float scaledThreat = Mathf.Max(0f, confirmedDamage) * DamageThreatMultiplier;
            return Mathf.Max(MinimumDamageThreat, scaledThreat);
        }


        /// <summary>
        /// 신규 컬럼이 없거나 0인 데이터에서는 호환 기본 전환 비율을 사용합니다.
        /// </summary>
        private static float ResolveThreatSwitchRatio(float value)
        {
            return value > 0f
                ? Mathf.Max(1f, value)
                : DefaultTargetSwitchThreatRatio;
        }

        /// <summary>
        /// 신규 컬럼이 없거나 0인 데이터에서는 호환 기본 최대 대상 수를 사용합니다.
        /// </summary>
        private static int ResolveMaxThreatTargets(int value)
        {
            return value > 0
                ? Mathf.Clamp(value, 1, MaximumThreatTargets)
                : DefaultMaxThreatTargets;
        }

        /// <summary>
        /// 0보다 큰 값을 사용하고, 유효하지 않으면 호환 기본값을 반환합니다.
        /// </summary>
        private static float ResolvePositive(float value, float fallback)
        {
            return value > 0f ? value : fallback;
        }
    }
}
