using System;
using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorVfxReferenceOption
    {
        public TableEditorTableDefinition ReferenceTable;
        public TableEditorReferenceItem Item;
        public string SourceLabel;
    }

    internal static class TableEditorVfxReferenceUtility
    {
        public const string EffectTabId = "effect";
        public const string ParticleTabId = "particle";

        public static bool IsTabbedVfxReference(TableEditorColumnDefinition column)
        {
            return IsTabbedVfxReference(column?.HeaderName);
        }

        public static bool IsTabbedVfxReference(string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
                return false;

            return string.Equals(headerName, "VfxUid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(headerName, "HitVfxUid", StringComparison.OrdinalIgnoreCase);
        }

        public static IReadOnlyList<SearchableDropdownUtility.OptionTab<TableEditorVfxReferenceOption>> BuildTabs()
        {
            List<SearchableDropdownUtility.Option<TableEditorVfxReferenceOption>> effectOptions = BuildOptions(GetEffectTable(), "Effect");
            List<SearchableDropdownUtility.Option<TableEditorVfxReferenceOption>> particleOptions = BuildOptions(GetParticleTable(), "Particle");

            return new[]
            {
                new SearchableDropdownUtility.OptionTab<TableEditorVfxReferenceOption>(EffectTabId, "Effect", effectOptions),
                new SearchableDropdownUtility.OptionTab<TableEditorVfxReferenceOption>(ParticleTabId, "Particle", particleOptions),
            };
        }

        public static string GetSelectedTabId(int uid)
        {
            if (TryFindItem(uid, out _, out TableEditorTableDefinition table))
            {
                if (string.Equals(table?.TableKey, ConfigAddressableTable.VfxParticle, StringComparison.OrdinalIgnoreCase))
                    return ParticleTabId;
            }

            return EffectTabId;
        }

        public static int GetSelectedIndex(IReadOnlyList<SearchableDropdownUtility.OptionTab<TableEditorVfxReferenceOption>> tabs, string tabId, int uid)
        {
            if (tabs == null)
                return -1;

            for (int tabIndex = 0; tabIndex < tabs.Count; tabIndex++)
            {
                SearchableDropdownUtility.OptionTab<TableEditorVfxReferenceOption> tab = tabs[tabIndex];
                if (!string.Equals(tab.Id, tabId, StringComparison.OrdinalIgnoreCase))
                    continue;

                IReadOnlyList<SearchableDropdownUtility.Option<TableEditorVfxReferenceOption>> options = tab.Options;
                if (options == null)
                    return -1;

                for (int optionIndex = 0; optionIndex < options.Count; optionIndex++)
                {
                    TableEditorVfxReferenceOption data = options[optionIndex].Data;
                    if (data?.Item != null && data.Item.Uid == uid)
                        return optionIndex;
                }

                return -1;
            }

            return -1;
        }

        public static bool Contains(int uid)
        {
            return TryFindItem(uid, out _, out _);
        }

        public static bool TryFindItem(int uid, out TableEditorReferenceItem item, out TableEditorTableDefinition table)
        {
            item = null;
            table = null;

            if (uid <= 0)
                return false;

            TableEditorTableDefinition effectTable = GetEffectTable();
            item = TableEditorReferenceCache.FindItem(effectTable, uid);
            if (item != null)
            {
                table = effectTable;
                return true;
            }

            TableEditorTableDefinition particleTable = GetParticleTable();
            item = TableEditorReferenceCache.FindItem(particleTable, uid);
            if (item != null)
            {
                table = particleTable;
                return true;
            }

            return false;
        }

        public static string BuildButtonText(int uid)
        {
            if (!TryFindItem(uid, out TableEditorReferenceItem item, out TableEditorTableDefinition table))
                return "Select Vfx";

            string sourceLabel = GetSourceLabel(table);
            return $"{item.Uid}  |  {item.DisplayName} [{sourceLabel}]";
        }

        public static string BuildCellText(int uid)
        {
            if (!TryFindItem(uid, out TableEditorReferenceItem item, out TableEditorTableDefinition table))
                return uid.ToString();

            return $"{uid} ({item.DisplayName} [{GetSourceLabel(table)}])";
        }

        public static void JumpToReference(Action<TableEditorTableDefinition, int> onJumpToReference, int uid)
        {
            if (onJumpToReference == null || uid <= 0)
                return;

            if (TryFindItem(uid, out _, out TableEditorTableDefinition table))
                onJumpToReference.Invoke(table, uid);
        }

        private static TableEditorTableDefinition GetEffectTable()
        {
            return TableEditorRegistry.FindByKey(ConfigAddressableTable.VfxEffect);
        }

        private static TableEditorTableDefinition GetParticleTable()
        {
            return TableEditorRegistry.FindByKey(ConfigAddressableTable.VfxParticle);
        }

        private static List<SearchableDropdownUtility.Option<TableEditorVfxReferenceOption>> BuildOptions(TableEditorTableDefinition table, string sourceLabel)
        {
            IReadOnlyList<TableEditorReferenceItem> items = TableEditorReferenceCache.GetItems(table);
            List<SearchableDropdownUtility.Option<TableEditorVfxReferenceOption>> options = new List<SearchableDropdownUtility.Option<TableEditorVfxReferenceOption>>(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                TableEditorReferenceItem item = items[i];
                TableEditorVfxReferenceOption option = new TableEditorVfxReferenceOption
                {
                    ReferenceTable = table,
                    Item = item,
                    SourceLabel = sourceLabel,
                };

                options.Add(new SearchableDropdownUtility.Option<TableEditorVfxReferenceOption>(
                    item.Uid.ToString(),
                    $"{item.DisplayName} [{sourceLabel}]",
                    option));
            }

            return options;
        }

        private static string GetSourceLabel(TableEditorTableDefinition table)
        {
            if (string.Equals(table?.TableKey, ConfigAddressableTable.VfxParticle, StringComparison.OrdinalIgnoreCase))
                return "Particle";

            return "Effect";
        }
    }
}
