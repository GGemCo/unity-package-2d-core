using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// CharacterStat - Combat 관련 공개 API/유틸리티 모음.
    /// (계산/발행 로직은 Modules 쪽에서만 담당합니다.)
    /// </summary>
    public partial class CharacterStat
    {
        /// <summary>
        /// 현재 공격속도를 100 기준 퍼센트 값으로 반환합니다.
        /// </summary>
        public float GetCurrentAttackSpeed() => TotalAttackSpeed.Value / 100f;

        /// <summary>
        /// 기본 공격속도를 지정한 값만큼 증가시키고 전체 스탯을 다시 계산합니다.
        /// </summary>
        /// <param name="value">기본 공격속도에 더할 양수 값입니다.</param>
        /// <remarks>
        /// 장비, 패시브, Affect Modifier는 변경하지 않으며 정수 범위를 넘으면 <see cref="int.MaxValue"/>로 제한합니다.
        /// </remarks>
        public void IncreaseCurrentAttackSpeed(int value)
        {
            if (value <= 0)
                return;

            long increasedValue = (long)BaseAttackSpeed + value;
            BaseAttackSpeed = increasedValue >= int.MaxValue
                ? int.MaxValue
                : (int)increasedValue;
            RecalculateStats();
        }

        /// <summary>
        /// 기본 공격속도를 지정한 값으로 교체하고 전체 스탯을 다시 계산합니다.
        /// </summary>
        /// <param name="value">새로 적용할 1 이상의 기본 공격속도입니다.</param>
        /// <remarks>장비, 패시브, Affect Modifier는 유지되므로 최종 공격속도는 지정값과 다를 수 있습니다.</remarks>
        public void SetCurrentAttackSpeed(int value)
        {
            if (value <= 0)
                return;

            BaseAttackSpeed = value;
            RecalculateStats();
        }

        /// <summary>
        /// 데미지 타입에 대응되는 현재 기본 속성 데미지 값을 반환합니다.
        /// </summary>
        /// <param name="damageType">조회할 데미지 타입입니다.</param>
        /// <returns>화염/냉기/번개/독이면 해당 속성 데미지 값을, 그 외에는 0을 반환합니다.</returns>
        public long GetElementDamageValue(ConfigCommon.DamageType damageType)
        {
            return damageType switch
            {
                ConfigCommon.DamageType.Fire => TotalDamageFire.Value,
                ConfigCommon.DamageType.Cold => TotalDamageCold.Value,
                ConfigCommon.DamageType.Lightning => TotalDamageLightning.Value,
                ConfigCommon.DamageType.Poison => TotalDamagePoison.Value,
                _ => 0L
            };
        }

        /// <summary>
        /// 데미지 타입에 대응되는 현재 속성 게이지 누적력을 반환합니다.
        /// </summary>
        /// <param name="damageType">조회할 속성 타입입니다.</param>
        /// <returns>화염/냉기/번개/독이면 해당 속성 게이지 누적력을, 그 외에는 0을 반환합니다.</returns>
        public long GetElementGaugeValue(ConfigCommon.DamageType damageType)
        {
            return damageType switch
            {
                ConfigCommon.DamageType.Fire => TotalElementGaugeFire.Value,
                ConfigCommon.DamageType.Cold => TotalElementGaugeCold.Value,
                ConfigCommon.DamageType.Lightning => TotalElementGaugeLightning.Value,
                ConfigCommon.DamageType.Poison => TotalElementGaugePoison.Value,
                _ => 0L
            };
        }

        /// <summary>
        /// 현재 Total 값을 기준으로 1회 공격의 최종 데미지를 계산합니다(크리티컬 포함).
        /// </summary>
        /// <returns>전역 계산 정책이 반영된 일반 공격 데미지입니다.</returns>
        protected long CalculateFinalAttack()
        {
            CalculateManager calculateManager = CalculateManager.GetActive();
            if (calculateManager != null)
                return calculateManager.CalculateBasicAttackDamage(this);

            return CalculateFinalAttackFallback();
        }

        /// <summary>
        /// 기본 콤보 공격 단계에 설정된 공식 정보를 기준으로 최종 데미지를 계산합니다.
        /// </summary>
        /// <param name="target">피격 대상 캐릭터입니다. Poly 공식의 대상 변수 계산에 사용됩니다.</param>
        /// <param name="settings">현재 기본 콤보 공격 단계의 공식 설정입니다.</param>
        /// <returns>전역 계산 정책과 콤보 공식 설정이 반영된 일반 공격 데미지입니다.</returns>
        protected long CalculateFinalAttack(CharacterBase target, in AttackComboDamageFormulaSettings settings)
        {
            CalculateManager calculateManager = CalculateManager.GetActive();
            if (calculateManager != null)
                return calculateManager.CalculateBasicAttackDamage(this, target, settings);

            if (!settings.useCustomFormula)
                return CalculateFinalAttackFallback();

            double baseDamage = settings.ResolveBaseDamage(this);
            double resolved = baseDamage * settings.ResolveDamageRate() * settings.ResolveEventMultiplier() * settings.ResolveOptionMultiplier();
            if (resolved <= 0d)
                return 0L;

            if (settings.rollCritical)
            {
                float criticalChance = Mathf.Clamp01(TotalCriticalProbability.Value / 100f);
                if (Random.value < criticalChance)
                {
                    float critMultiplier = Mathf.Max(1f, TotalCriticalDamage.Value / 100f);
                    resolved *= critMultiplier;
                }
            }

            if (resolved >= long.MaxValue)
                return long.MaxValue;

            return (long)System.Math.Round(resolved);
        }

        /// <summary>
        /// CalculateManager가 준비되지 않은 초기 타이밍에서 사용할 기본 공격 데미지 폴백을 계산합니다.
        /// </summary>
        /// <returns>현재 ResolvedAtk와 크리티컬 스탯만 반영한 기본 공격 데미지입니다.</returns>
        private long CalculateFinalAttackFallback()
        {
            long baseAttack = ResolvedAtk.Value;
            if (baseAttack <= 0) return 0;

            float finalDamage = baseAttack;
            float criticalChance = Mathf.Clamp01(TotalCriticalProbability.Value / 100f);

            if (!(Random.value < criticalChance)) return Mathf.RoundToInt(finalDamage);

            float critMultiplier = Mathf.Max(1f, TotalCriticalDamage.Value / 100f);
            finalDamage *= critMultiplier;

            return Mathf.RoundToInt(finalDamage);
        }

        /// <summary>
        /// 현재 Total 값을 기준으로 1회 공격의 기대 데미지(기대값)를 계산합니다.
        /// </summary>
        public float CalculateExpectedAttack()
        {
            long baseAttack = ResolvedAtk.Value;
            if (baseAttack <= 0) return 0;

            float criticalChance = Mathf.Clamp01(TotalCriticalProbability.Value / 100f);
            float critMultiplier = Mathf.Max(1f, TotalCriticalDamage.Value / 100f);

            // 기대값 = 일반 공격 * (1 - 크리확) + 크리티컬 공격 * (크리확)
            float expectedDamage = baseAttack * (1 - criticalChance) + (baseAttack * critMultiplier * criticalChance);
            return expectedDamage;
        }
    }
}
