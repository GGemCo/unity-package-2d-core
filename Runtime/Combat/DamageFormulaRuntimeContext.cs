namespace GGemCo2DCore
{
    /// <summary>
    /// 실제 적중 대상이 확정된 시점에 데미지 공식을 다시 계산하기 위한 런타임 입력 스냅샷입니다.
    /// </summary>
    /// <remarks>
    /// Core 패키지가 Skill 패키지 타입을 직접 참조하지 않도록, 스킬 이벤트 실행 시점에 필요한 값만 순수 데이터로 전달합니다.
    /// Projectile, Laser처럼 발사 시점과 적중 시점의 대상이 달라질 수 있는 전투 이벤트에서 재사용합니다.
    /// </remarks>
    public sealed class DamageFormulaRuntimeContext
    {
        /// <summary>실행할 Poly 공식 키입니다. 비어 있으면 기본 배율 공식을 사용합니다.</summary>
        public string FormulaKey { get; }

        /// <summary>공식에 전달할 기본 데미지입니다.</summary>
        public double BaseDamage { get; }

        /// <summary>스킬 데미지 배율입니다. 100%는 1.0입니다.</summary>
        public double SkillDamageRate { get; }

        /// <summary>이벤트 단위 데미지 배율입니다.</summary>
        public double EventMultiplier { get; }

        /// <summary>스킬 실행 옵션 단위 데미지 배율입니다.</summary>
        public double OptionMultiplier { get; }

        /// <summary>공식에 전달할 버프 배율입니다. 버프가 없으면 0입니다.</summary>
        public double BuffRate { get; }

        /// <summary>데미지 타입입니다.</summary>
        public ConfigCommon.DamageType DamageType { get; }

        /// <summary>크리티컬 판정을 적용할지 여부입니다.</summary>
        public bool RollCritical { get; }

        /// <summary>
        /// 실제 적중 대상 기준 재계산용 공식 입력 스냅샷을 생성합니다.
        /// </summary>
        /// <param name="formulaKey">실행할 Poly 공식 키입니다.</param>
        /// <param name="baseDamage">공식에 전달할 기본 데미지입니다.</param>
        /// <param name="skillDamageRate">스킬 데미지 배율입니다.</param>
        /// <param name="eventMultiplier">이벤트 단위 데미지 배율입니다.</param>
        /// <param name="optionMultiplier">실행 옵션 단위 데미지 배율입니다.</param>
        /// <param name="buffRate">공식에 전달할 버프 배율입니다.</param>
        /// <param name="damageType">데미지 타입입니다.</param>
        /// <param name="rollCritical">크리티컬 판정을 적용할지 여부입니다.</param>
        public DamageFormulaRuntimeContext(
            string formulaKey,
            double baseDamage,
            double skillDamageRate,
            double eventMultiplier,
            double optionMultiplier,
            double buffRate,
            ConfigCommon.DamageType damageType,
            bool rollCritical)
        {
            FormulaKey = formulaKey ?? string.Empty;
            BaseDamage = baseDamage;
            SkillDamageRate = skillDamageRate;
            EventMultiplier = eventMultiplier;
            OptionMultiplier = optionMultiplier;
            BuffRate = buffRate;
            DamageType = damageType;
            RollCritical = rollCritical;
        }
    }
}
