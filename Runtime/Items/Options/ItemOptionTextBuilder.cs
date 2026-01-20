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
        /// 고정(Base) 옵션만 UI용 문자열(멀티라인)로 생성한다.
        /// </summary>
        public static string BuildBaseOptionsText(int itemUid, LocalizationManager loc)
        {
            if (itemUid <= 0) return string.Empty;

            var tlm = TableLoaderManager.Instance;
            if (tlm == null) return string.Empty;

            var resolver = new ItemOptionResolver(tlm);
            var entries = resolver.ResolveBaseOptions(itemUid);
            return BuildFromEntries(entries, loc);
        }

        /// <summary>
        /// 인스턴스에 롤된(Random) 옵션만 UI용 문자열(멀티라인)로 생성한다.
        /// </summary>
        /// <remarks>
        /// - <paramref name="instanceId"/>가 0이거나 조회 실패 시 빈 문자열을 반환한다.
        /// - Affect 옵션은 설명(여러 줄 가능)을 그대로 출력한다.
        /// </remarks>
        public static string BuildRandomOptionsText(long instanceId, LocalizationManager loc)
        {
            if (instanceId <= 0) return string.Empty;

            var tlm = TableLoaderManager.Instance;
            if (tlm == null) return string.Empty;

            ItemInstanceInfo instance = null;
            var store = SceneGame.Instance?.saveDataManager?.ItemInstances;
            if (store != null)
                store.TryGet(instanceId, out instance);

            if (instance == null) return string.Empty;

            var resolver = new ItemOptionResolver(tlm);
            var entries = resolver.ResolveRolledOptions(instance);
            return BuildFromEntries(entries, loc);
        }

        /// <summary>
        /// 신규 옵션 시스템(고정 옵션 + 인스턴스 랜덤 옵션)을 기반으로 옵션 문자열을 생성한다.
        /// </summary>
        /// <remarks>
        /// - <paramref name="instanceId"/>가 0이면 고정 옵션(TableItemBaseOption)만 표시한다.
        /// - <paramref name="instanceId"/>가 유효하면 고정 옵션 + 인스턴스에 롤된 Affix 옵션을 합쳐 표시한다.
        /// - Affect 옵션은 설명(여러 줄 가능)을 그대로 출력한다.
        /// </remarks>
        public static string BuildOptions(int itemUid, long instanceId, LocalizationManager loc)
        {
            if (itemUid <= 0) return string.Empty;

            var tlm = TableLoaderManager.Instance;
            if (tlm == null) return string.Empty;

            var resolver = new ItemOptionResolver(tlm);

            // instanceId가 유효하더라도, 런타임에 DB가 없거나 조회 실패할 수 있으므로 안전하게 처리한다.
            ItemInstanceInfo instance = null;
            if (instanceId > 0)
            {
                var store = SceneGame.Instance?.saveDataManager?.ItemInstances;
                if (store != null)
                    store.TryGet(instanceId, out instance);
            }

            List<ItemOptionEntry> entries = instance != null
                ? resolver.ResolveFinalOptions(instance)
                : resolver.ResolveBaseOptions(itemUid);

            return BuildFromEntries(entries, loc);
        }

        private static bool TryFormatOptionLine(ItemOptionEntry entry, LocalizationManager loc, out string line)
        {
            line = null;
            if (!entry.IsValid) return false;

            if (entry.Kind == ItemOptionKind.Affect)
            {
                // Affect는 TargetId가 UID(int)라고 가정
                if (!int.TryParse(entry.TargetId, out var affectUid) || affectUid <= 0)
                    return false;

                string desc = AffectBridge.DescriptionProvider.GetDescription(affectUid);
                if (string.IsNullOrWhiteSpace(desc)) return false;

                // Affect 설명은 여러 줄일 수 있으므로 그대로 출력
                line = desc;
                return true;
            }

            string name = ResolveOptionName(entry.Kind, entry.TargetId, loc);
            if (string.IsNullOrEmpty(name)) return false;

            string valueText = ResolveValueText(entry.Op, entry.Value);
            line = $"{name}: {valueText}";
            return true;
        }

        private static string BuildFromEntries(List<ItemOptionEntry> entries, LocalizationManager loc)
        {
            if (entries == null || entries.Count == 0)
                return string.Empty;

            var lines = new List<string>(entries.Count);
            foreach (var itemOptionEntry in entries)
            {
                if (TryFormatOptionLine(itemOptionEntry, loc, out var line))
                    lines.Add(line);
            }
            return lines.Count <= 0 ? string.Empty : string.Join("\n", lines);
        }

        private static string ResolveOptionName(ItemOptionKind kind, string targetId, LocalizationManager loc)
        {
            if (string.IsNullOrEmpty(targetId)) return string.Empty;

            var tlm = TableLoaderManager.Instance;
            if (tlm != null)
            {
                switch (kind)
                {
                    case ItemOptionKind.Stat:
                    {
                        var stat = tlm.TableStat?.GetDataById(targetId);
                        if (stat != null && !string.IsNullOrEmpty(stat.Name)) return stat.Name;
                        break;
                    }
                    case ItemOptionKind.State:
                    {
                        var state = tlm.TableState?.GetDataById(targetId);
                        if (state != null && !string.IsNullOrEmpty(state.Name)) return state.Name;
                        break;
                    }
                    case ItemOptionKind.DamageType:
                    {
                        var dt = tlm.TableDamageType?.GetDataById(targetId);
                        if (dt != null && !string.IsNullOrEmpty(dt.Name)) return dt.Name;
                        break;
                    }
                }
            }

            // 마지막 fallback: 로컬라이제이션 기반 이름(기존 StatusName 키를 재사용)
            return loc != null ? loc.GetStatusNameByKey(targetId) : targetId;
        }

        private static string ResolveValueText(ConfigCommon.SuffixType suffixType, float value)
        {
            // ItemConstants.StatusSuffixFormats를 그대로 사용
            foreach (var suffix in ItemConstants.StatusSuffixFormats.Keys)
            {
                if (suffixType == suffix)
                    return string.Format(ItemConstants.StatusSuffixFormats[suffix], value);
            }
            return $"{value}";
        }
    }
}
