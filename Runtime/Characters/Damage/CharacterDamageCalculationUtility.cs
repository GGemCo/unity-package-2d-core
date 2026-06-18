using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 피격 파이프라인에서 사용하는 순수 데미지 판정과 분해 결과 보정을 제공합니다.
    /// </summary>
    internal static class CharacterDamageCalculationUtility
    {
        /// <summary>
        /// 확정 타격 후속 처리 대상인지 확인합니다.
        /// </summary>
        /// <param name="metadataDamage">검사할 데미지 메타데이터입니다.</param>
        /// <returns>즉시 타격이면 <see langword="true"/>, 지속 피해 Tick이면 <see langword="false"/>입니다.</returns>
        public static bool ShouldProcessConfirmedAttackHit(MetadataDamage metadataDamage)
        {
            return HasNonDotDamagePart(metadataDamage);
        }

        /// <summary>
        /// 현재 데미지가 가드 또는 저스트 가드 판정 대상인지 확인합니다.
        /// </summary>
        /// <param name="metadataDamage">검사할 데미지 메타데이터입니다.</param>
        /// <returns>가드 판정을 수행해야 하면 <see langword="true"/>입니다.</returns>
        public static bool ShouldEvaluateGuardResolution(MetadataDamage metadataDamage)
        {
            return HasNonDotDamagePart(metadataDamage);
        }

        /// <summary>
        /// 데미지 분해 결과에 면역 처리된 파트가 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="breakdown">검사할 데미지 분해 결과입니다.</param>
        /// <returns>면역 파트가 하나 이상 있으면 <see langword="true"/>입니다.</returns>
        public static bool HasAnyImmuneDamagePart(DamageCalculationBreakdown breakdown)
        {
            if (breakdown == null || !breakdown.HasParts)
                return false;

            IReadOnlyList<DamagePartResult> parts = breakdown.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].IsImmune)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 가드 등 후처리로 변경된 최종 데미지에 맞춰 파트별 결과를 비례 보정합니다.
        /// </summary>
        /// <param name="source">보정 전 데미지 분해 결과입니다.</param>
        /// <param name="targetFinalDamage">후처리가 반영된 전체 최종 데미지입니다.</param>
        /// <returns>전체 합계에 맞춰 보정된 데미지 분해 결과입니다.</returns>
        public static DamageCalculationBreakdown ScaleFinalDamage(
            DamageCalculationBreakdown source,
            long targetFinalDamage)
        {
            if (source == null || !source.HasParts)
                return source;

            long sourceFinalDamage = source.TotalFinalDamage;
            if (sourceFinalDamage == targetFinalDamage)
                return source;

            var scaled = new DamageCalculationBreakdown();
            IReadOnlyList<DamagePartResult> parts = source.Parts;
            long safeTargetFinalDamage = targetFinalDamage > 0L ? targetFinalDamage : 0L;
            long assignedFinalDamage = 0L;
            int lastPositivePartIndex = FindLastPositiveDamagePartIndex(parts);

            for (int i = 0; i < parts.Count; i++)
            {
                DamagePartResult part = parts[i];
                long scaledFinalDamage = 0L;

                if (sourceFinalDamage > 0L && part.FinalDamage > 0L && safeTargetFinalDamage > 0L)
                {
                    scaledFinalDamage = i == lastPositivePartIndex
                        ? safeTargetFinalDamage - assignedFinalDamage
                        : (long)System.Math.Round(
                            part.FinalDamage * (double)safeTargetFinalDamage / sourceFinalDamage);
                    if (scaledFinalDamage < 0L)
                        scaledFinalDamage = 0L;

                    assignedFinalDamage += scaledFinalDamage;
                }

                scaled.AddPart(new DamagePartResult(
                    part.RawDamage,
                    scaledFinalDamage,
                    part.DamageType,
                    ScaleAttackerElementDamage(part, scaledFinalDamage),
                    part.IsImmune,
                    part.AppliedDefaultDamage,
                    part.IsDot));
            }

            return scaled;
        }

        /// <summary>
        /// 지속 피해가 아닌 데미지 파트가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="metadataDamage">검사할 데미지 메타데이터입니다.</param>
        /// <returns>즉시 타격 파트가 존재하면 <see langword="true"/>입니다.</returns>
        private static bool HasNonDotDamagePart(MetadataDamage metadataDamage)
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

        /// <summary>
        /// 보정된 파트 데미지 비율에 맞춰 공격자 속성 데미지 기준값을 보정합니다.
        /// </summary>
        private static long ScaleAttackerElementDamage(in DamagePartResult part, long scaledFinalDamage)
        {
            if (part.AttackerElementDamage <= 0L)
                return 0L;
            if (part.FinalDamage <= 0L)
                return scaledFinalDamage > 0L ? part.AttackerElementDamage : 0L;
            if (scaledFinalDamage <= 0L)
                return 0L;

            double scaled = part.AttackerElementDamage * (scaledFinalDamage / (double)part.FinalDamage);
            if (scaled <= 0d)
                return 0L;
            if (scaled >= long.MaxValue)
                return long.MaxValue;

            return (long)System.Math.Round(scaled);
        }

        /// <summary>
        /// 최종 데미지가 있는 마지막 파트의 인덱스를 찾습니다.
        /// </summary>
        private static int FindLastPositiveDamagePartIndex(IReadOnlyList<DamagePartResult> parts)
        {
            if (parts == null)
                return -1;

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                if (parts[i].FinalDamage > 0L)
                    return i;
            }

            return -1;
        }
    }
}
