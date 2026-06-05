namespace GGemCo2DCore
{
    /// <summary>
    /// 데미지 계산 결과입니다.
    /// </summary>
    public readonly struct DamageCalculationResult
    {
        /// <summary>계산 요청 시 전달된 원본 데미지입니다.</summary>
        public readonly long OriginalDamage;

        /// <summary>계산 정책을 모두 반영한 최종 데미지입니다.</summary>
        public readonly long FinalDamage;

        /// <summary>계산 도중 데미지가 0 이하로 내려갔는지 여부입니다.</summary>
        public readonly bool WasReducedToZeroOrLess;

        /// <summary>0 이하 데미지 보정 정책으로 기본 데미지를 적용했는지 여부입니다.</summary>
        public readonly bool AppliedDefaultDamage;

        /// <summary>기본 데미지가 적용되지 않아 면역으로 처리해야 하는지 여부입니다.</summary>
        public readonly bool IsImmune;

        /// <summary>이번 계산에 사용한 데미지 타입입니다.</summary>
        public readonly ConfigCommon.DamageType DamageType;

        /// <summary>
        /// 데미지 계산 결과를 생성합니다.
        /// </summary>
        /// <param name="originalDamage">계산 요청 시 전달된 원본 데미지입니다.</param>
        /// <param name="finalDamage">계산 정책을 모두 반영한 최종 데미지입니다.</param>
        /// <param name="wasReducedToZeroOrLess">계산 도중 데미지가 0 이하로 내려갔는지 여부입니다.</param>
        /// <param name="appliedDefaultDamage">0 이하 데미지 보정 정책으로 기본 데미지를 적용했는지 여부입니다.</param>
        /// <param name="isImmune">기본 데미지가 적용되지 않아 면역으로 처리해야 하는지 여부입니다.</param>
        /// <param name="damageType">이번 계산에 사용한 데미지 타입입니다.</param>
        public DamageCalculationResult(
            long originalDamage,
            long finalDamage,
            bool wasReducedToZeroOrLess,
            bool appliedDefaultDamage,
            bool isImmune,
            ConfigCommon.DamageType damageType)
        {
            OriginalDamage = originalDamage;
            FinalDamage = finalDamage;
            WasReducedToZeroOrLess = wasReducedToZeroOrLess;
            AppliedDefaultDamage = appliedDefaultDamage;
            IsImmune = isImmune;
            DamageType = damageType;
        }
    }
}
