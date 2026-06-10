using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 공식 변수 디버그에서 표시할 단일 기여 정보입니다.
    /// </summary>
    public readonly struct DamageFormulaVariableDebugRecord
    {
        /// <summary>
        /// 공식 변수 디버그 레코드를 생성합니다.
        /// </summary>
        /// <param name="variableKey">공식 변수 ID입니다.</param>
        /// <param name="value">해당 출처가 제공한 최종 변수 값입니다.</param>
        /// <param name="sourceType">변수 제공 출처 타입입니다.</param>
        /// <param name="sourceName">변수 제공 출처 이름입니다.</param>
        /// <param name="ownerRole">공격자/피격자/현재 대상 구분입니다.</param>
        public DamageFormulaVariableDebugRecord(
            string variableKey,
            double value,
            StatModifierDebugSourceType sourceType,
            string sourceName,
            string ownerRole)
        {
            VariableKey = string.IsNullOrWhiteSpace(variableKey) ? string.Empty : variableKey.Trim();
            Value = Sanitize(value);
            SourceType = sourceType;
            SourceName = string.IsNullOrWhiteSpace(sourceName) ? sourceType.ToString() : sourceName.Trim();
            OwnerRole = string.IsNullOrWhiteSpace(ownerRole) ? string.Empty : ownerRole.Trim();
        }

        /// <summary>공식 변수 ID입니다.</summary>
        public string VariableKey { get; }

        /// <summary>해당 출처가 제공한 최종 변수 값입니다.</summary>
        public double Value { get; }

        /// <summary>변수 제공 출처 타입입니다.</summary>
        public StatModifierDebugSourceType SourceType { get; }

        /// <summary>변수 제공 출처 이름입니다.</summary>
        public string SourceName { get; }

        /// <summary>공격자/피격자/현재 대상 구분입니다.</summary>
        public string OwnerRole { get; }

        /// <summary>
        /// 공식 계산을 방해하지 않도록 NaN/Infinity 값을 0으로 보정합니다.
        /// </summary>
        private static double Sanitize(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? 0d : value;
        }
    }

    /// <summary>
    /// 공식 변수 디버그 HUD에 표시할 출처별 합산 정보입니다.
    /// </summary>
    public readonly struct DamageFormulaVariableDebugLine
    {
        /// <summary>
        /// 공식 변수 디버그 표시 항목을 생성합니다.
        /// </summary>
        public DamageFormulaVariableDebugLine(string variableKey, double itemValue, double skillValue, double affectValue, double finalValue)
        {
            VariableKey = string.IsNullOrWhiteSpace(variableKey) ? string.Empty : variableKey.Trim();
            ItemValue = itemValue;
            SkillValue = skillValue;
            AffectValue = affectValue;
            FinalValue = finalValue;
        }

        /// <summary>공식 변수 ID입니다.</summary>
        public string VariableKey { get; }

        /// <summary>아이템/장비 출처의 합산 값입니다.</summary>
        public double ItemValue { get; }

        /// <summary>패시브 스킬 출처의 합산 값입니다.</summary>
        public double SkillValue { get; }

        /// <summary>Affect 출처의 합산 값입니다.</summary>
        public double AffectValue { get; }

        /// <summary>디버그 대상 출처를 모두 합산한 최종 변수 값입니다.</summary>
        public double FinalValue { get; }
    }

    /// <summary>
    /// 공식 변수 Provider가 HUD/데미지 스냅샷용 출처 정보를 제공하기 위한 선택 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 기존 <see cref="IDamageFormulaVariableProvider"/> 계산 계약을 변경하지 않고,
    /// 구현체가 선택적으로 공식 변수 기여도를 노출할 때 사용합니다.
    /// </remarks>
    public interface IDamageFormulaVariableDebugProvider
    {
        /// <summary>
        /// 현재 Provider가 보유한 공식 변수 기여도를 수집합니다.
        /// </summary>
        /// <param name="attacker">공격자 캐릭터입니다.</param>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <param name="results">수집 결과를 추가할 목록입니다.</param>
        void CollectDamageFormulaVariableDebugRecords(
            CharacterBase attacker,
            CharacterBase target,
            List<DamageFormulaVariableDebugRecord> results);
    }
}
