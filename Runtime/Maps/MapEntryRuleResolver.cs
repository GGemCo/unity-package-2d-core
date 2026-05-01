using System.Collections.Generic;
using System.Globalization;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 입장 요청에 map_entry_rule 테이블을 적용하여 실제 입장 맵을 결정합니다.
    /// </summary>
    public sealed class MapEntryRuleResolver
    {
        private readonly TableLoaderManager _tableLoaderManager;
        private readonly LicenseManager _licenseManager;

        /// <summary>
        /// 테이블 로더와 라이센스 매니저를 기반으로 맵 입장 규칙 해석기를 생성합니다.
        /// </summary>
        /// <param name="tableLoaderManager">map_entry_rule 및 license 테이블을 제공하는 테이블 로더입니다.</param>
        /// <param name="licenseManager">라이센스 저장 상태를 조회할 매니저입니다.</param>
        public MapEntryRuleResolver(TableLoaderManager tableLoaderManager, LicenseManager licenseManager)
        {
            _tableLoaderManager = tableLoaderManager;
            _licenseManager = licenseManager;
        }

        /// <summary>
        /// 요청 맵 UID에 매칭되는 첫 번째 규칙을 적용하여 실제 입장 맵 UID를 반환합니다.
        /// </summary>
        /// <param name="requestMapUid">플레이어가 원래 입장하려던 맵 UID입니다.</param>
        /// <returns>규칙이 매칭되면 대상 맵 UID를, 없으면 요청 맵 UID를 반환합니다.</returns>
        public int ResolveTargetMapUid(int requestMapUid)
        {
            if (requestMapUid <= 0 || _tableLoaderManager?.TableMapEntryRule == null)
            {
                return requestMapUid;
            }

            IReadOnlyList<StruckTableMapEntryRule> rules =
                _tableLoaderManager.TableMapEntryRule.GetRulesByRequestMapUid(requestMapUid);
            for (int i = 0; i < rules.Count; i++)
            {
                StruckTableMapEntryRule rule = rules[i];
                if (IsMatched(rule))
                {
                    return rule.TargetMapUid;
                }
            }

            return requestMapUid;
        }

        /// <summary>
        /// 단일 맵 입장 규칙의 조건이 현재 라이센스 상태와 일치하는지 확인합니다.
        /// </summary>
        /// <param name="rule">검사할 맵 입장 규칙입니다.</param>
        /// <returns>조건이 일치하면 true를 반환합니다.</returns>
        private bool IsMatched(StruckTableMapEntryRule rule)
        {
            if (rule == null || !rule.Enabled || rule.TargetMapUid <= 0)
            {
                return false;
            }

            if (rule.ConditionLicenseUid <= 0)
            {
                return true;
            }

            StruckTableLicense license = _tableLoaderManager.GetLicenseData(rule.ConditionLicenseUid, false);
            if (license == null || string.IsNullOrWhiteSpace(license.Key))
            {
                return false;
            }

            return CompareLicense(rule, license.Key);
        }

        /// <summary>
        /// 규칙의 비교 타입에 따라 라이센스 저장값을 비교합니다.
        /// </summary>
        /// <param name="rule">비교 조건을 담은 맵 입장 규칙입니다.</param>
        /// <param name="licenseKey">조회할 라이센스 Key입니다.</param>
        /// <returns>비교 결과가 참이면 true를 반환합니다.</returns>
        private bool CompareLicense(StruckTableMapEntryRule rule, string licenseKey)
        {
            string currentValue = string.Empty;
            bool exists = _licenseManager != null && _licenseManager.TryGetValue(licenseKey, out currentValue);
            switch (rule.CompareType)
            {
                case MapEntryRuleConstants.CompareType.Exists:
                    return exists;
                case MapEntryRuleConstants.CompareType.NotExists:
                    return !exists;
                case MapEntryRuleConstants.CompareType.Equals:
                    return exists && string.Equals(currentValue, rule.CompareValue, System.StringComparison.OrdinalIgnoreCase);
                case MapEntryRuleConstants.CompareType.NotEquals:
                    return exists && !string.Equals(currentValue, rule.CompareValue, System.StringComparison.OrdinalIgnoreCase);
                case MapEntryRuleConstants.CompareType.Greater:
                    return exists && CompareNumber(currentValue, rule.CompareValue, (left, right) => left > right);
                case MapEntryRuleConstants.CompareType.GreaterOrEqual:
                    return exists && CompareNumber(currentValue, rule.CompareValue, (left, right) => left >= right);
                case MapEntryRuleConstants.CompareType.Less:
                    return exists && CompareNumber(currentValue, rule.CompareValue, (left, right) => left < right);
                case MapEntryRuleConstants.CompareType.LessOrEqual:
                    return exists && CompareNumber(currentValue, rule.CompareValue, (left, right) => left <= right);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 두 문자열 값을 숫자로 변환한 뒤 지정한 비교 함수를 적용합니다.
        /// </summary>
        /// <param name="leftValue">현재 저장된 라이센스 값입니다.</param>
        /// <param name="rightValue">테이블에 입력된 비교 기준 값입니다.</param>
        /// <param name="comparison">숫자 비교 함수입니다.</param>
        /// <returns>두 값이 숫자로 변환되고 비교 결과가 참이면 true를 반환합니다.</returns>
        private static bool CompareNumber(string leftValue, string rightValue, System.Func<float, float, bool> comparison)
        {
            return TryParseFloat(leftValue, out float left) &&
                   TryParseFloat(rightValue, out float right) &&
                   comparison(left, right);
        }

        /// <summary>
        /// 문자열을 고정 문화권 기준 float 값으로 변환합니다.
        /// </summary>
        /// <param name="value">변환할 문자열입니다.</param>
        /// <param name="result">변환에 성공하면 float 값이 설정됩니다.</param>
        /// <returns>변환에 성공하면 true를 반환합니다.</returns>
        private static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result);
        }
    }
}
