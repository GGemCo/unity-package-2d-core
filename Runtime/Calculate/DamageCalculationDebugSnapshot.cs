using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 마지막 데미지 계산 결과를 디버그 HUD에 표시하기 위한 읽기 전용 스냅샷입니다.
    /// </summary>
    public readonly struct DamageCalculationDebugSnapshot
    {
        /// <summary>
        /// 데미지 계산 디버그 스냅샷을 생성합니다.
        /// </summary>
        public DamageCalculationDebugSnapshot(
            string formulaKey,
            string formulaType,
            double baseDamage,
            double skillDamageRate,
            double eventMultiplier,
            double optionMultiplier,
            ConfigCommon.DamageType damageType,
            long rawDamage,
            long finalDamage,
            bool appliedDefaultDamage,
            bool isImmune,
            DateTime recordedAt)
        {
            FormulaKey = string.IsNullOrWhiteSpace(formulaKey) ? "-" : formulaKey;
            FormulaType = string.IsNullOrWhiteSpace(formulaType) ? "-" : formulaType;
            BaseDamage = baseDamage;
            SkillDamageRate = skillDamageRate;
            EventMultiplier = eventMultiplier;
            OptionMultiplier = optionMultiplier;
            DamageType = damageType;
            RawDamage = rawDamage;
            FinalDamage = finalDamage;
            AppliedDefaultDamage = appliedDefaultDamage;
            IsImmune = isImmune;
            RecordedAt = recordedAt;
        }

        /// <summary>공식 키입니다. Poly 공식이 아니면 '-'입니다.</summary>
        public string FormulaKey { get; }

        /// <summary>계산 타입 또는 공식 분류입니다.</summary>
        public string FormulaType { get; }

        /// <summary>계산에 사용한 기준 데미지입니다.</summary>
        public double BaseDamage { get; }

        /// <summary>스킬/기본 공격 데미지 배율입니다.</summary>
        public double SkillDamageRate { get; }

        /// <summary>이벤트 단위 배율입니다.</summary>
        public double EventMultiplier { get; }

        /// <summary>실행 옵션 단위 배율입니다.</summary>
        public double OptionMultiplier { get; }

        /// <summary>데미지 타입입니다.</summary>
        public ConfigCommon.DamageType DamageType { get; }

        /// <summary>기본 데미지 정책 적용 전 데미지입니다.</summary>
        public long RawDamage { get; }

        /// <summary>최종 보정이 끝난 데미지입니다.</summary>
        public long FinalDamage { get; }

        /// <summary>0 이하 데미지 기본값이 적용되었는지 여부입니다.</summary>
        public bool AppliedDefaultDamage { get; }

        /// <summary>최종 데미지가 면역/0 처리되었는지 여부입니다.</summary>
        public bool IsImmune { get; }

        /// <summary>스냅샷 기록 시각입니다.</summary>
        public DateTime RecordedAt { get; }
    }
}
