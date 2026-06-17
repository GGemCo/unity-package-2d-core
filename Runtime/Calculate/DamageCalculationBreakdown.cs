using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 하나의 공격을 구성하는 개별 데미지 파트 계산 결과입니다.
    /// </summary>
    /// <remarks>
    /// 물리 기본 피해와 화염/냉기/번개/독 추가 피해를 같은 공격 안에서 분리해 추적하기 위해 사용합니다.
    /// HP 차감은 전체 합산 결과를 사용하고, 속성 게이지나 속성별 표시 로직은 각 파트를 기준으로 처리합니다.
    /// </remarks>
    public readonly struct DamagePartResult
    {
        /// <summary>계산 전 원본 데미지입니다.</summary>
        public readonly long RawDamage;

        /// <summary>저항과 보정 정책이 적용된 최종 데미지입니다.</summary>
        public readonly long FinalDamage;

        /// <summary>이 파트가 표현하는 데미지 타입입니다.</summary>
        public readonly ConfigCommon.DamageType DamageType;

        /// <summary>공격자가 보유한 속성 데미지 스탯에서 유래한 값입니다.</summary>
        public readonly long AttackerElementDamage;

        /// <summary>이 파트가 저항 또는 면역 정책으로 무효화되었는지 여부입니다.</summary>
        public readonly bool IsImmune;

        /// <summary>0 이하 데미지 보정 정책으로 기본 데미지를 적용했는지 여부입니다.</summary>
        public readonly bool AppliedDefaultDamage;

        /// <summary>지속 피해 파트인지 여부입니다.</summary>
        public readonly bool IsDot;

        /// <summary>
        /// 데미지 파트 계산 결과를 생성합니다.
        /// </summary>
        /// <param name="rawDamage">계산 전 원본 데미지입니다.</param>
        /// <param name="finalDamage">저항과 보정 정책이 적용된 최종 데미지입니다.</param>
        /// <param name="damageType">이 파트가 표현하는 데미지 타입입니다.</param>
        /// <param name="attackerElementDamage">공격자가 보유한 속성 데미지 스탯에서 유래한 값입니다.</param>
        /// <param name="isImmune">이 파트가 저항 또는 면역 정책으로 무효화되었는지 여부입니다.</param>
        /// <param name="appliedDefaultDamage">0 이하 데미지 보정 정책으로 기본 데미지를 적용했는지 여부입니다.</param>
        /// <param name="isDot">지속 피해 파트인지 여부입니다.</param>
        public DamagePartResult(
            long rawDamage,
            long finalDamage,
            ConfigCommon.DamageType damageType,
            long attackerElementDamage = 0L,
            bool isImmune = false,
            bool appliedDefaultDamage = false,
            bool isDot = false)
        {
            RawDamage = rawDamage;
            FinalDamage = finalDamage;
            DamageType = damageType;
            AttackerElementDamage = attackerElementDamage;
            IsImmune = isImmune;
            AppliedDefaultDamage = appliedDefaultDamage;
            IsDot = isDot;
        }
    }

    /// <summary>
    /// 하나의 공격에 대한 총 데미지와 속성별 데미지 파트 목록입니다.
    /// </summary>
    /// <remarks>
    /// 런타임 중에는 파트 목록을 외부에서 임의 수정하지 않도록 전용 메서드로만 구성합니다.
    /// 데미지 처리 루프에서 불필요한 복사를 피하기 위해 읽기 전용 인터페이스로 노출합니다.
    /// </remarks>
    public sealed class DamageCalculationBreakdown
    {
        private readonly List<DamagePartResult> _parts = new List<DamagePartResult>(4);

        /// <summary>계산 전 전체 원본 데미지 합계입니다.</summary>
        public long TotalRawDamage { get; private set; }

        /// <summary>저항과 보정 정책이 적용된 전체 최종 데미지 합계입니다.</summary>
        public long TotalFinalDamage { get; private set; }

        /// <summary>대표 데미지 타입입니다. 기존 단일 타입 기반 코드와의 호환을 위해 유지합니다.</summary>
        public ConfigCommon.DamageType RepresentativeDamageType { get; private set; } = ConfigCommon.DamageType.None;

        /// <summary>개별 데미지 파트 목록입니다.</summary>
        public IReadOnlyList<DamagePartResult> Parts => _parts;

        /// <summary>유효한 데미지 파트가 있는지 여부입니다.</summary>
        public bool HasParts => _parts.Count > 0;

        /// <summary>
        /// 데미지 파트를 추가하고 전체 합계를 갱신합니다.
        /// </summary>
        /// <param name="part">추가할 데미지 파트입니다.</param>
        public void AddPart(in DamagePartResult part)
        {
            if (part.DamageType == ConfigCommon.DamageType.None && part.RawDamage <= 0L && part.FinalDamage <= 0L)
                return;

            _parts.Add(part);
            TotalRawDamage = AddClamped(TotalRawDamage, part.RawDamage);
            TotalFinalDamage = AddClamped(TotalFinalDamage, part.FinalDamage);

            if (RepresentativeDamageType == ConfigCommon.DamageType.None && part.DamageType != ConfigCommon.DamageType.None)
                RepresentativeDamageType = part.DamageType;
        }

        /// <summary>
        /// 단일 데미지 계산 결과를 포함하는 분해 결과를 생성합니다.
        /// </summary>
        /// <param name="result">기존 단일 데미지 계산 결과입니다.</param>
        /// <returns>단일 파트를 가진 데미지 분해 결과입니다.</returns>
        public static DamageCalculationBreakdown FromSingle(in DamageCalculationResult result)
        {
            var breakdown = new DamageCalculationBreakdown();
            breakdown.AddPart(new DamagePartResult(
                result.OriginalDamage,
                result.FinalDamage,
                result.DamageType,
                0L,
                result.IsImmune,
                result.AppliedDefaultDamage));
            return breakdown;
        }

        private static long AddClamped(long a, long b)
        {
            if (b > 0L && a > long.MaxValue - b)
                return long.MaxValue;

            if (b < 0L && a < long.MinValue - b)
                return long.MinValue;

            return a + b;
        }
    }
}
