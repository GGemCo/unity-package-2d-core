using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 옵션을 UI용 문자열(멀티라인)로 변환하는 유틸리티.
    /// </summary>
    public static class ItemOptionTextBuilder
    {
        /// <summary>
        /// 현재 Core의 레거시(StatusID*/OptionType*) 컬럼을 기반으로 옵션 문자열을 생성한다.
        /// </summary>
        public static string BuildLegacyOptions(StruckTableItem item, LocalizationManager loc)
        {
            if (item == null) return string.Empty;

            var lines = new List<string>(8);

            TryAddLegacyLine(lines, item.StatusID1, item.StatusSuffix1, item.StatusValue1, loc);
            TryAddLegacyLine(lines, item.StatusID2, item.StatusSuffix2, item.StatusValue2, loc);

            TryAddLegacyLine(lines, item.OptionType1, item.OptionSuffix1, item.OptionValue1, loc);
            TryAddLegacyLine(lines, item.OptionType2, item.OptionSuffix2, item.OptionValue2, loc);
            TryAddLegacyLine(lines, item.OptionType3, item.OptionSuffix3, item.OptionValue3, loc);
            TryAddLegacyLine(lines, item.OptionType4, item.OptionSuffix4, item.OptionValue4, loc);
            TryAddLegacyLine(lines, item.OptionType5, item.OptionSuffix5, item.OptionValue5, loc);

            return lines.Count <= 0 ? string.Empty : string.Join("\n", lines);
        }

        private static void TryAddLegacyLine(List<string> lines, string statusId, ConfigCommon.SuffixType suffixType, float value, LocalizationManager loc)
        {
            if (string.IsNullOrEmpty(statusId)) return;

            if (statusId == ConfigCommon.StatusAffectId)
            {
                int affectUid = Mathf.RoundToInt(value);
                if (affectUid <= 0) return;

                string desc = AffectBridge.DescriptionProvider.GetDescription(affectUid);
                if (string.IsNullOrWhiteSpace(desc)) return;

                // Affect 설명은 여러 줄일 수 있으므로 그대로 추가
                lines.Add(desc);
                return;
            }

            string statusName = ResolveStatusName(statusId, loc);
            if (string.IsNullOrEmpty(statusName)) return;

            string valueText = ResolveValueText(suffixType, value);
            lines.Add($"{statusName}: {valueText}");
        }

        private static string ResolveValueText(ConfigCommon.SuffixType suffixType, float value)
        {
            // ItemConstants.StatusSuffixFormats를 그대로 사용
            foreach (var suffix in ItemConstants.StatusSuffixFormats.Keys)
            {
                if (suffixType == suffix)
                    return string.Format(ItemConstants.StatusSuffixFormats[suffix], value);
            }
            return value.ToString();
        }

        private static string ResolveStatusName(string statusId, LocalizationManager loc)
        {
            // UIWindowItemInfo.GetStatusName 로직을 재사용한 형태
            var tlm = TableLoaderManager.Instance;
            if (tlm != null)
            {
                var stat = tlm.TableStat?.GetDataById(statusId);
                if (stat != null && !string.IsNullOrEmpty(stat.Name)) return stat.Name;

                var damage = tlm.TableDamageType?.GetDataById(statusId);
                if (damage != null && !string.IsNullOrEmpty(damage.Name)) return damage.Name;

                var state = tlm.TableState?.GetDataById(statusId);
                if (state != null && !string.IsNullOrEmpty(state.Name)) return state.Name;
            }

            return loc != null ? loc.GetStatusNameByKey(statusId) : statusId;
        }
    }
}
