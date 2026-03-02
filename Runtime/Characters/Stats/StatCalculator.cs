using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스탯 최종 합산/계산 규칙을 담당하는 순수 로직(Stateless) 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 계산 규칙:
    /// - Base에 모든 Provider의 Flat(가산)을 먼저 합산한 뒤 Percent(%)를 적용합니다.
    /// - Percent는 100% 기준(예: 10이면 +10%, -20이면 -20%)으로 누적됩니다.
    /// - 최종 배율이 0 미만이 되면 0으로 클램프합니다.
    /// </remarks>
    public static class StatCalculator
    {
        /// <summary>
        /// 지정한 스탯 키에 대해 Base 값과 Provider들의 modifier를 합산하여 최종값을 계산합니다.
        /// </summary>
        /// <param name="statKey">계산할 스탯 키(예: STAT_ATK 등)입니다.</param>
        /// <param name="baseValue">기본(Base) 스탯 값입니다.</param>
        /// <param name="providers">Flat/Percent modifier를 제공하는 Provider 목록입니다.</param>
        /// <returns>(Base + Flat합) * (1 + Percent합/100) 규칙으로 계산된 최종값입니다.</returns>
        public static long CalculateFinal(
            string statKey,
            int baseValue,
            IReadOnlyList<IStatModifierProvider> providers)
        {
            int flat = 0;
            float percent = 0f;

            for (int i = 0; i < providers.Count; i++)
            {
                var p = providers[i];
                if (p == null) continue;

                if (p.Flat != null && p.Flat.TryGetValue(statKey, out var fv))
                    flat += fv;

                if (p.Percent != null && p.Percent.TryGetValue(statKey, out var pv))
                    percent += pv;
            }

            return CalculateFinalInternal(baseValue, flat, percent);
        }

        /// <summary>
        /// 특정 “가정(프로젝션) 버킷”의 modifier를 추가로 반영했을 때의 최종값을 계산합니다.
        /// - 실제 Provider 상태는 변경하지 않습니다.
        /// </summary>
        /// <param name="statKey">계산할 스탯 키(예: STAT_ATK 등)입니다.</param>
        /// <param name="baseValue">기본(Base) 스탯 값입니다.</param>
        /// <param name="flatProjected">추가로 가정할 Flat(가산) modifier 사전입니다.</param>
        /// <param name="percentProjected">추가로 가정할 Percent(%) modifier 사전입니다.</param>
        /// <param name="providersExcludingProjectedBucket">가정 버킷을 제외하고 합산할 Provider 목록입니다.</param>
        /// <returns>가정 버킷을 포함하여 계산된 최종값입니다.</returns>
        public static long CalculateFinalProjected(
            string statKey,
            int baseValue,
            IReadOnlyDictionary<string, int> flatProjected,
            IReadOnlyDictionary<string, float> percentProjected,
            IReadOnlyList<IStatModifierProvider> providersExcludingProjectedBucket)
        {
            int flat = 0;
            float percent = 0f;

            for (int i = 0; i < providersExcludingProjectedBucket.Count; i++)
            {
                var p = providersExcludingProjectedBucket[i];
                if (p == null) continue;

                if (p.Flat != null && p.Flat.TryGetValue(statKey, out var fv))
                    flat += fv;

                if (p.Percent != null && p.Percent.TryGetValue(statKey, out var pv))
                    percent += pv;
            }

            if (flatProjected != null && flatProjected.TryGetValue(statKey, out var fp))
                flat += fp;
            if (percentProjected != null && percentProjected.TryGetValue(statKey, out var pp))
                percent += pp;

            return CalculateFinalInternal(baseValue, flat, percent);
        }

        /// <summary>
        /// 계산 핵심 로직입니다.
        /// </summary>
        /// <param name="baseValue">기본(Base) 값입니다.</param>
        /// <param name="flatBonus">Flat(가산) 보너스 합입니다.</param>
        /// <param name="percentBonus">Percent(%) 보너스 합입니다(100 기준).</param>
        /// <returns>규칙에 따라 계산된 최종값입니다.</returns>
        private static long CalculateFinalInternal(int baseValue, int flatBonus, float percentBonus)
        {
            float finalMultiplier = 1f + (percentBonus / 100f);
            if (finalMultiplier < 0f) finalMultiplier = 0f;

            // 기존 CharacterStat과 동일: (base+flat) * multiplier 를 long으로 캐스팅(소수점은 절삭).
            return (long)((baseValue + flatBonus) * finalMultiplier);
        }
    }
}