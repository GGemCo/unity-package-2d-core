using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorColumnDefinition
    {
        public string HeaderName;
        public Type ValueType;
        public MemberInfo MemberInfo;
        public bool ExistsInRowType;
        public bool ExistsInFileHeader;

        public bool IsUidColumn => string.Equals(HeaderName, "Uid", StringComparison.OrdinalIgnoreCase);
        public bool IsReferenceCandidate => !IsUidColumn && (HeaderName.EndsWith("Uid", StringComparison.Ordinal) || HeaderName.EndsWith("Id", StringComparison.Ordinal));
    }

    internal sealed class TableEditorTableDefinition
    {
        public string TableKey;
        public string AssetPath;
        public string DisplayName;
        public Type TableType;
        public Type RowType;
        public Func<string, object> CreateTableInstanceAndLoad;

        public IReadOnlyList<TableEditorColumnDefinition> BuildColumns(IReadOnlyList<string> fileHeaders)
        {
            Dictionary<string, MemberInfo> memberMap = TableEditorReflectionUtility
                .GetEditableMembers(RowType)
                .ToDictionary(static m => m.Name, StringComparer.OrdinalIgnoreCase);

            List<TableEditorColumnDefinition> columns = new List<TableEditorColumnDefinition>();
            HashSet<string> added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (fileHeaders != null)
            {
                for (int i = 0; i < fileHeaders.Count; i++)
                {
                    string header = fileHeaders[i];
                    if (string.IsNullOrWhiteSpace(header) || !added.Add(header))
                        continue;

                    memberMap.TryGetValue(header, out MemberInfo memberInfo);
                    columns.Add(new TableEditorColumnDefinition
                    {
                        HeaderName = header,
                        MemberInfo = memberInfo,
                        ValueType = memberInfo != null ? TableEditorReflectionUtility.GetMemberType(memberInfo) : typeof(string),
                        ExistsInFileHeader = true,
                        ExistsInRowType = memberInfo != null,
                    });
                }
            }

            foreach (var pair in memberMap)
            {
                if (!added.Add(pair.Key))
                    continue;

                columns.Add(new TableEditorColumnDefinition
                {
                    HeaderName = pair.Value.Name,
                    MemberInfo = pair.Value,
                    ValueType = TableEditorReflectionUtility.GetMemberType(pair.Value),
                    ExistsInFileHeader = false,
                    ExistsInRowType = true,
                });
            }

            return columns;
        }
    }

    internal static class TableEditorRegistry
    {
        private static List<TableEditorTableDefinition> _tables;
        private static readonly Dictionary<string, string> ReferenceAliasByHeader = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ItemUid", ConfigAddressableTable.Item },
            { "MonsterUid", ConfigAddressableTable.Monster },
            { "NpcUid", ConfigAddressableTable.Npc },
            { "MapUid", ConfigAddressableTable.Map },
            { "AnimationUid", ConfigAddressableTable.Animation },
            { "EffectUid", ConfigAddressableTable.Effect },
            { "ProjectileUid", ConfigAddressableTable.Projectile },
            { "BgmUid", ConfigAddressableTable.Sound },
            { "SoundUid", ConfigAddressableTable.Sound },
            { "OpenWindowUid", ConfigAddressableTable.Window },
            { "CloseWindowUid", ConfigAddressableTable.Window },
            { "ResultItemUid", ConfigAddressableTable.Item },
            { "SourceItemUid", ConfigAddressableTable.Item },
            { "NeedItemUid1", ConfigAddressableTable.Item },
            { "NeedItemUid2", ConfigAddressableTable.Item },
            { "NeedItemUid3", ConfigAddressableTable.Item },
            { "NeedItemUid4", ConfigAddressableTable.Item },
            { "NeedItemUid5", ConfigAddressableTable.Item },
            { "UseGroupUid", ConfigAddressableTable.ItemUse },
            { "PlayerDeadSpawnUid", ConfigAddressableTable.Map },
        };

        public static IReadOnlyList<TableEditorTableDefinition> GetAll()
        {
            if (_tables != null)
                return _tables;

            _tables = BuildRegistry();
            return _tables;
        }

        public static TableEditorTableDefinition FindByKey(string tableKey)
        {
            return GetAll().FirstOrDefault(t => string.Equals(t.TableKey, tableKey, StringComparison.OrdinalIgnoreCase));
        }

        public static TableEditorTableDefinition FindReferenceTable(string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
                return null;

            if (ReferenceAliasByHeader.TryGetValue(headerName, out string mappedKey))
                return FindByKey(mappedKey);

            string normalized = headerName;
            if (normalized.EndsWith("Uid", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(0, normalized.Length - 3);
            else if (normalized.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(0, normalized.Length - 2);

            string key = ToSnakeCase(normalized);
            return FindByKey(key);
        }

        private static List<TableEditorTableDefinition> BuildRegistry()
        {
            List<TableEditorTableDefinition> result = new List<TableEditorTableDefinition>();
            var addressableInfos = ConfigAddressableTable.All;

            Type defaultTableType = typeof(DefaultTable<>);
            Assembly runtimeAssembly = typeof(DefaultTable<>).Assembly;

            foreach (Type type in runtimeAssembly.GetTypes())
            {
                if (type.IsAbstract)
                    continue;

                Type baseType = type.BaseType;
                if (baseType == null || !baseType.IsGenericType)
                    continue;

                if (baseType.GetGenericTypeDefinition() != defaultTableType)
                    continue;

                Type rowType = baseType.GetGenericArguments()[0];
                object tableInstance = null;
                try
                {
                    tableInstance = Activator.CreateInstance(type);
                }
                catch
                {
                    continue;
                }

                if (!(tableInstance is ITableParser parser))
                    continue;

                string key = parser.Key;
                if (string.IsNullOrWhiteSpace(key) || string.Equals(key, ConfigAddressableTable.None, StringComparison.OrdinalIgnoreCase))
                    continue;

                var addressable = addressableInfos.FirstOrDefault(a => string.Equals(a.Etc1, key, StringComparison.OrdinalIgnoreCase));
                if (addressable == null)
                    continue;

                result.Add(new TableEditorTableDefinition
                {
                    TableKey = key,
                    AssetPath = addressable.Path,
                    DisplayName = key,
                    TableType = type,
                    RowType = rowType,
                    CreateTableInstanceAndLoad = content =>
                    {
                        object instance = Activator.CreateInstance(type);
                        if (instance is ITableParser tableParser)
                        {
                            tableParser.LoadData(content);
                            return instance;
                        }

                        return null;
                    }
                });
            }

            result.Sort(static (a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        private static string ToSnakeCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            List<char> chars = new List<char>(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsUpper(c) && i > 0)
                    chars.Add('_');

                chars.Add(char.ToLowerInvariant(c));
            }

            return new string(chars.ToArray());
        }
    }
}
