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
        protected long CalculateFinalAttack()
        {
            long baseAttack = TotalAtk.Value;
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
            long baseAttack = TotalAtk.Value;
            if (baseAttack <= 0) return 0;

            float criticalChance = Mathf.Clamp01(TotalCriticalProbability.Value / 100f);
            float critMultiplier = Mathf.Max(1f, TotalCriticalDamage.Value / 100f);

            // 기대값 = 일반 공격 * (1 - 크리확) + 크리티컬 공격 * (크리확)
            float expectedDamage = baseAttack * (1 - criticalChance) + (baseAttack * critMultiplier * criticalChance);
            return expectedDamage;
        }
    }
}