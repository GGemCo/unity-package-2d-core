namespace GGemCo2DCore
{
    /// <summary>
    /// Poly 또는 기본 공식 기반 스킬 데미지 계산 요청 값입니다.
    /// </summary>
    public readonly struct DamageFormulaRequest
    {
        public readonly CharacterBase Attacker;
        public readonly CharacterBase Target;
        public readonly string FormulaKey;
        public readonly double BaseDamage;
        public readonly double SkillDamageRate;
        public readonly double EventMultiplier;
        public readonly double OptionMultiplier;
        public readonly double BuffRate;
        public readonly ConfigCommon.DamageType DamageType;
        public readonly bool RollCritical;

        /// <summary>
        /// 데미지 공식 요청을 생성합니다.
        /// </summary>
        /// <param name="attacker">공격자 캐릭터입니다.</param>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <param name="formulaKey">실행할 Poly 공식 키입니다. 비어 있으면 기존 기본 공식을 사용합니다.</param>
        /// <param name="baseDamage">공식에 전달할 기본 데미지입니다.</param>
        /// <param name="skillDamageRate">스킬 데미지 배율입니다. 100%는 1.0으로 전달합니다.</param>
        /// <param name="eventMultiplier">이벤트 단위 데미지 배율입니다.</param>
        /// <param name="optionMultiplier">실행 옵션 데미지 배율입니다.</param>
        /// <param name="buffRate">공식에 전달할 기본 버프 배율입니다. 버프가 없으면 0입니다.</param>
        /// <param name="damageType">데미지 타입입니다.</param>
        /// <param name="rollCritical">크리티컬 판정을 적용할지 여부입니다.</param>
        public DamageFormulaRequest(
            CharacterBase attacker,
            CharacterBase target,
            string formulaKey,
            double baseDamage,
            double skillDamageRate,
            double eventMultiplier,
            double optionMultiplier,
            double buffRate,
            ConfigCommon.DamageType damageType,
            bool rollCritical)
        {
            Attacker = attacker;
            Target = target;
            FormulaKey = formulaKey;
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
