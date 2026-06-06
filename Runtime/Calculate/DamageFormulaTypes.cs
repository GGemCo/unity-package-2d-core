using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 데미지 계산에 사용할 공식 타입입니다.
    /// </summary>
    public enum DamageFormulaType
    {
        /// <summary>일반 물리 공격 공식입니다.</summary>
        BasicPhysical = 0,
        /// <summary>전달된 기본 데미지에 배율만 적용하는 공식입니다.</summary>
        MultiplierOnly = 1,
    }

    /// <summary>
    /// 데미지 공식 실행에 필요한 입력값입니다.
    /// </summary>
    public readonly struct DamageFormulaContext
    {
        public readonly CharacterStat Attacker;
        public readonly CharacterBase Target;
        public readonly double BaseDamage;
        public readonly float EventMultiplier;
        public readonly float OptionMultiplier;
        public readonly ConfigCommon.DamageType DamageType;
        public readonly bool RollCritical;

        /// <summary>
        /// 데미지 공식 컨텍스트를 생성합니다.
        /// </summary>
        public DamageFormulaContext(
            CharacterStat attacker,
            CharacterBase target,
            double baseDamage,
            float eventMultiplier,
            float optionMultiplier,
            ConfigCommon.DamageType damageType,
            bool rollCritical)
        {
            Attacker = attacker;
            Target = target;
            BaseDamage = baseDamage;
            EventMultiplier = eventMultiplier;
            OptionMultiplier = optionMultiplier;
            DamageType = damageType;
            RollCritical = rollCritical;
        }
    }

    /// <summary>
    /// 데미지 공식의 공통 계약입니다.
    /// </summary>
    public interface IDamageFormula
    {
        /// <summary>
        /// 전달된 컨텍스트를 기준으로 저항 적용 전 데미지를 계산합니다.
        /// </summary>
        /// <param name="context">데미지 계산 입력값입니다.</param>
        /// <returns>저항 적용 전 데미지입니다.</returns>
        long Calculate(in DamageFormulaContext context);
    }

    /// <summary>
    /// 일반 물리 공격 공식입니다.
    /// </summary>
    public sealed class BasicPhysicalDamageFormula : IDamageFormula
    {
        /// <summary>
        /// 기본 항목 공격력과 스탯 항목 공격 스탯을 분리해서 읽은 뒤 합산하여 데미지를 계산합니다.
        /// </summary>
        public long Calculate(in DamageFormulaContext context)
        {
            CharacterStat attacker = context.Attacker;
            if (attacker == null) return 0L;

            double damage = attacker.TotalBaseAtk.Value + attacker.TotalStatAtk.Value;
            damage = ApplyCriticalIfNeeded(damage, attacker, context.RollCritical);
            return RoundToLong(damage);
        }

        /// <summary>
        /// 크리티컬 판정을 적용합니다.
        /// </summary>
        private static double ApplyCriticalIfNeeded(double damage, CharacterStat attacker, bool rollCritical)
        {
            if (!rollCritical || attacker == null || damage <= 0d) return damage;

            float criticalChance = Mathf.Clamp01(attacker.TotalCriticalProbability.Value / 100f);
            if (!(Random.value < criticalChance)) return damage;

            float criticalMultiplier = Mathf.Max(1f, attacker.TotalCriticalDamage.Value / 100f);
            return damage * criticalMultiplier;
        }

        /// <summary>
        /// 실수 데미지를 long 범위로 변환합니다.
        /// </summary>
        private static long RoundToLong(double value)
        {
            if (value <= 0d) return 0L;
            if (value >= long.MaxValue) return long.MaxValue;
            return (long)System.Math.Round(value);
        }
    }

    /// <summary>
    /// 기본 데미지와 배율만 사용하는 공식입니다.
    /// </summary>
    public sealed class MultiplierOnlyDamageFormula : IDamageFormula
    {
        /// <summary>
        /// 기본 데미지에 이벤트 배율과 옵션 배율을 적용합니다.
        /// </summary>
        public long Calculate(in DamageFormulaContext context)
        {
            float safeEventMultiplier = Mathf.Max(0f, context.EventMultiplier);
            float safeOptionMultiplier = context.OptionMultiplier > 0f ? context.OptionMultiplier : 1f;
            double resolved = System.Math.Max(0d, context.BaseDamage) * safeEventMultiplier * safeOptionMultiplier;
            if (resolved <= 0d) return 0L;
            if (resolved >= long.MaxValue) return long.MaxValue;
            return (long)System.Math.Round(resolved);
        }
    }
}
