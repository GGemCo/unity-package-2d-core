using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 확정 타격 시 공격자의 속성 게이지 누적력 스탯을 피격 대상의 속성 게이지에 반영합니다.
    /// </summary>
    /// <remarks>
    /// 속성 데미지와 속성 게이지는 서로 독립된 시스템입니다.
    /// 이 서비스는 <c>BASE_ELEMENT_GAUGE_*</c> 스탯만 읽으며, <c>BASE_DAMAGE_*</c> 또는 실제 HP 데미지량은 참조하지 않습니다.
    /// </remarks>
    public static class ElementGaugeOnHitApplier
    {
        private static readonly ConfigCommon.DamageType[] ElementTypes =
        {
            ConfigCommon.DamageType.Fire,
            ConfigCommon.DamageType.Cold,
            ConfigCommon.DamageType.Lightning,
            ConfigCommon.DamageType.Poison,
        };

        /// <summary>
        /// 확정 타격 메타데이터를 기준으로 속성 게이지 누적력을 대상에게 적용합니다.
        /// </summary>
        /// <param name="attacker">게이지 누적력 스탯을 보유한 공격자입니다.</param>
        /// <param name="target">게이지가 누적될 피격 대상입니다.</param>
        /// <param name="metadataDamage">이번 확정 타격 메타데이터입니다.</param>
        /// <returns>하나 이상의 속성 게이지 상태가 변경되었으면 <see langword="true"/>입니다.</returns>
        public static bool Apply(CharacterBase attacker, CharacterBase target, MetadataDamage metadataDamage)
        {
            if (attacker == null || target == null || metadataDamage == null)
                return false;

            if (!ShouldApplyForDamage(metadataDamage))
                return false;

            CharacterElementGaugeController targetGaugeController = target.ElementGaugeController;
            if (targetGaugeController == null)
                return false;

            bool changed = false;
            for (int i = 0; i < ElementTypes.Length; i++)
            {
                ConfigCommon.DamageType elementType = ElementTypes[i];
                long gaugeValue = Math.Max(0L, attacker.GetElementGaugeValue(elementType));
                if (gaugeValue <= 0L)
                    continue;

                ElementGaugeAccumulationResult result = targetGaugeController.Accumulate(
                    elementType,
                    gaugeValue,
                    attacker.gameObject,
                    metadataDamage);

                changed |= result.GaugeChanged || result.ThresholdReached || result.RepeatedGaugeInput;
            }

            return changed;
        }

        /// <summary>
        /// 이번 데미지가 속성 게이지 누적 대상인지 확인합니다.
        /// </summary>
        /// <param name="metadataDamage">검사할 데미지 메타데이터입니다.</param>
        /// <returns>즉시 타격이면 <see langword="true"/>, 지속 피해이면 <see langword="false"/>입니다.</returns>
        private static bool ShouldApplyForDamage(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null || metadataDamage.IsDamageOverTime)
                return false;

            DamageCalculationBreakdown breakdown = metadataDamage.DamageBreakdown;
            if (breakdown == null || !breakdown.HasParts)
                return true;

            IReadOnlyList<DamagePartResult> parts = breakdown.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (!parts[i].IsDot)
                    return true;
            }

            return false;
        }
    }
}
