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
