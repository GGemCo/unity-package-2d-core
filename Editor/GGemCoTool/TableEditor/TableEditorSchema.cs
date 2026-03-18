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
        public TableEditorTableDefinition ReferenceTable;

        public bool IsUidColumn => string.Equals(HeaderName, "Uid", StringComparison.OrdinalIgnoreCase);
        public bool IsReferenceCandidate => !IsUidColumn && ReferenceTable != null && ValueType == typeof(int);
    }

    internal sealed class TableEditorTableDefinition
    {
        public string ModuleName;
        public string PackageName;
        public string TableKey;
        public string AssetPath;
        public string DisplayName;
        public Type TableType;
        public Type RowType;
        public Func<string, object> CreateTableInstanceAndLoad;
        public Action ReloadAction;
        public Func<string, TableEditorTableDefinition> ResolveReferenceTable;

        public string QualifiedDisplayName => string.IsNullOrWhiteSpace(PackageName)
            ? DisplayName
            : $"{PackageName} / {DisplayName}";

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
                    columns.Add(CreateColumn(this, header, memberInfo, true));
                }
            }

            foreach (KeyValuePair<string, MemberInfo> pair in memberMap)
            {
                if (!added.Add(pair.Key))
                    continue;

                columns.Add(CreateColumn(this, pair.Value.Name, pair.Value, false));
            }

            return columns;
        }

        public TableEditorTableDefinition TryResolveReferenceTable(string headerName)
        {
            if (ResolveReferenceTable != null)
            {
                TableEditorTableDefinition explicitTable = ResolveReferenceTable(headerName);
                if (explicitTable != null)
                    return explicitTable;
            }

            return TableEditorRegistry.FindReferenceTable(headerName);
        }

        private static TableEditorColumnDefinition CreateColumn(TableEditorTableDefinition owner, string headerName, MemberInfo memberInfo, bool existsInHeader)
        {
            Type valueType = memberInfo != null
                ? TableEditorReflectionUtility.GetMemberType(memberInfo)
                : typeof(string);

            return new TableEditorColumnDefinition
            {
                HeaderName = headerName,
                MemberInfo = memberInfo,
                ValueType = valueType,
                ExistsInFileHeader = existsInHeader,
                ExistsInRowType = memberInfo != null,
                ReferenceTable = owner.TryResolveReferenceTable(headerName),
            };
        }
    }

    internal static class TableEditorRegistry
    {
        private static readonly Dictionary<string, string> ReferenceAliasByHeader = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ItemUid", ConfigAddressableTable.Item },
            { "MonsterUid", ConfigAddressableTable.Monster },
            { "NpcUid", ConfigAddressableTable.Npc },
            { "MapUid", ConfigAddressableTable.Map },
            { "AnimationUid", ConfigAddressableTable.Animation },
            { "EffectUid", ConfigAddressableTable.Effect },
            { "ProjectileUid", ConfigAddressableTable.Projectile },
            { "CrowdControlUid", ConfigAddressableTable.CrowdControl },
            { "OpenWindowUid", ConfigAddressableTable.Window },
            { "CloseWindowUid", ConfigAddressableTable.Window },
            { "SoundUid", ConfigAddressableTable.Sound },
            { "BgmUid", ConfigAddressableTable.Sound },
            { "ApplyAffectUid", "affect" },
            { "AffectUid", "affect" },
        };

        private static List<TableEditorTableDefinition> _tables;
        private static List<ITableEditorModule> _modules;

        public static IReadOnlyList<TableEditorTableDefinition> GetAll()
        {
            if (_tables != null)
                return _tables;

            EnsureModules();
            _tables = new List<TableEditorTableDefinition>(64);
            for (int i = 0; i < _modules.Count; i++)
                _tables.AddRange(_modules[i].BuildDefinitions());

            _tables = _tables
                .Where(static t => t != null && !string.IsNullOrWhiteSpace(t.TableKey) && !string.IsNullOrWhiteSpace(t.AssetPath))
                .OrderBy(static t => t.PackageName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static t => t.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return _tables;
        }

        public static IReadOnlyList<string> GetPackages()
        {
            return GetAll()
                .Select(static t => t.PackageName)
                .Where(static p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
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

        private static void EnsureModules()
        {
            if (_modules != null)
                return;

            _modules = new List<ITableEditorModule>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                    continue;

                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    if (type == null || type.IsAbstract || type.IsInterface)
                        continue;
                    if (!typeof(ITableEditorModule).IsAssignableFrom(type))
                        continue;

                    try
                    {
                        if (Activator.CreateInstance(type) is ITableEditorModule module)
                            _modules.Add(module);
                    }
                    catch
                    {
                        // ignore invalid module instantiation
                    }
                }
            }

            _modules = _modules
                .GroupBy(static m => m.GetType().FullName, StringComparer.Ordinal)
                .Select(static g => g.First())
                .OrderBy(static m => m.PackageName, StringComparer.OrdinalIgnoreCase)
                .ToList();
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
