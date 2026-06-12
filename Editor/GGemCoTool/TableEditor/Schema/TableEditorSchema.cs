using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    public sealed class TableEditorColumnDefinition
    {
        public string HeaderName;
        public Type ValueType;
        public MemberInfo MemberInfo;
        public bool ExistsInRowType;
        public bool ExistsInFileHeader;
        public TableEditorTableDefinition ReferenceTable;
        public List<TableEditorReferenceRule> ReferenceRules;

        public bool IsUidColumn => string.Equals(HeaderName, "Uid", StringComparison.OrdinalIgnoreCase);
        public bool IsReferenceCandidate => ResolveReferenceRule(null)?.ValueKind == TableEditorReferenceValueKind.Uid && ValueType == typeof(int);
        public bool IsMultiReferenceCandidate => ResolveReferenceRule(null)?.ValueKind == TableEditorReferenceValueKind.Uid
            && ValueType != null
            && ValueType.IsArray
            && ValueType.GetElementType() == typeof(int);
        public bool HasReferenceCandidate => ReferenceRules != null && ReferenceRules.Count > 0;

        public TableEditorReferenceRule ResolveReferenceRule(TableEditorDocumentRow row)
        {
            if (ReferenceRules == null || ReferenceRules.Count == 0)
                return null;

            for (int i = 0; i < ReferenceRules.Count; i++)
            {
                TableEditorReferenceRule rule = ReferenceRules[i];
                if (rule != null && rule.IsEnabled(row))
                    return rule;
            }

            return ReferenceRules[0];
        }

        public TableEditorTableDefinition GetReferenceTable(TableEditorReferenceRule rule)
        {
            if (rule == null)
                return ReferenceTable;

            return TableEditorRegistry.FindByKey(rule.TargetTableKey) ?? ReferenceTable;
        }
    }

    public sealed class TableEditorTableDefinition
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

            TableEditorTableDefinition referenceTable = owner.TryResolveReferenceTable(headerName);
            List<TableEditorReferenceRule> referenceRules = TableEditorRegistry.BuildReferenceRules(owner, headerName, memberInfo, referenceTable);

            return new TableEditorColumnDefinition
            {
                HeaderName = headerName,
                MemberInfo = memberInfo,
                ValueType = valueType,
                ExistsInFileHeader = existsInHeader,
                ExistsInRowType = memberInfo != null,
                ReferenceTable = referenceTable,
                ReferenceRules = referenceRules,
            };
        }
    }

    public static class TableEditorRegistry
    {
        private static readonly Dictionary<string, string> ReferenceAliasByHeader = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ItemUid", ConfigAddressableTable.Item },
            { "ResultItemUid", ConfigAddressableTable.Item },
            { "NeedItemUid1", ConfigAddressableTable.Item },
            { "NeedItemUid2", ConfigAddressableTable.Item },
            { "NeedItemUid3", ConfigAddressableTable.Item },
            { "NeedItemUid4", ConfigAddressableTable.Item },
            { "SourceItemUid", ConfigAddressableTable.Item },
            { "MonsterUid", ConfigAddressableTable.Monster },
            { "DeathSkillMonsterUid", "skill_monster" },
            { "NpcUid", ConfigAddressableTable.Npc },
            { "MapUid", ConfigAddressableTable.Map },
            { "RequestMapUid", ConfigAddressableTable.Map },
            { "TargetMapUid", ConfigAddressableTable.Map },
            { "FallbackMapUid", ConfigAddressableTable.Map },
            { "AnimationUid", ConfigAddressableTable.Animation },
            { "VfxUid", ConfigAddressableTable.Vfx },
            { "HitVfxUid", ConfigAddressableTable.Vfx },
            { "CandidateVfxResourceUid", ConfigAddressableTable.VfxEffect },
            { "ProjectileUid", ConfigAddressableTable.Projectile },
            { "CrowdControlUid", ConfigAddressableTable.CrowdControl },
            { "LicenseUid", ConfigAddressableTable.License },
            { "ConditionLicenseUid", ConfigAddressableTable.License },
            { "OpenWindowUid", ConfigAddressableTable.Window },
            { "CloseWindowUid", ConfigAddressableTable.Window },
            { "SoundUid", ConfigAddressableTable.Sound },
            { "ChargeSoundUid", ConfigAddressableTable.Sound },
            { "BgmUid", ConfigAddressableTable.Sound },
            { "TransitionCutsceneUid", ConfigAddressableTable.Cutscene },
            { "PhaseStartCutsceneUid", ConfigAddressableTable.Cutscene },
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
                .GroupBy(static t => t.TableKey, StringComparer.OrdinalIgnoreCase)
                .Select(static g => g.First())
                .OrderBy(static t => t.PackageName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static t => t.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return _tables;
        }

        public static void Invalidate()
        {
            _tables = null;
            _modules = null;
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

        public static List<TableEditorReferenceRule> BuildReferenceRules(TableEditorTableDefinition owner, string headerName, MemberInfo memberInfo, TableEditorTableDefinition directReferenceTable)
        {
            List<TableEditorReferenceRule> rules = new List<TableEditorReferenceRule>();
            Type valueType = memberInfo != null ? TableEditorReflectionUtility.GetMemberType(memberInfo) : typeof(string);

            if (directReferenceTable != null)
            {
                rules.Add(new TableEditorReferenceRule
                {
                    TargetTableKey = directReferenceTable.TableKey,
                    ValueKind = valueType == typeof(string) ? TableEditorReferenceValueKind.StringId : TableEditorReferenceValueKind.Uid,
                    IsEnabledForRow = _ => true,
                });
            }

            if (owner != null && string.Equals(owner.TableKey, "affect_modifier", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(headerName, "StatId", StringComparison.OrdinalIgnoreCase))
                {
                    rules.Clear();
                    rules.Add(CreateConditionalStringIdRule(ConfigAddressableTable.Stat, "Stat"));
                }
                else if (string.Equals(headerName, "DamageTypeId", StringComparison.OrdinalIgnoreCase))
                {
                    rules.Clear();
                    rules.Add(CreateConditionalStringIdRule(ConfigAddressableTable.DamageType, "Damage"));
                }
                else if (string.Equals(headerName, "ScalingStatId", StringComparison.OrdinalIgnoreCase))
                {
                    rules.Clear();
                    rules.Add(CreateConditionalStringIdRule(ConfigAddressableTable.Stat, "Damage"));
                }
                else if (string.Equals(headerName, "HealScalingStatId", StringComparison.OrdinalIgnoreCase))
                {
                    rules.Clear();
                    rules.Add(CreateConditionalStringIdRule(ConfigAddressableTable.Stat, "Heal"));
                }
                else if (string.Equals(headerName, "StateId", StringComparison.OrdinalIgnoreCase))
                {
                    rules.Clear();
                    rules.Add(CreateConditionalStringIdRule(ConfigAddressableTable.State, "State"));
                }
                else if (string.Equals(headerName, "AffectUid", StringComparison.OrdinalIgnoreCase))
                {
                    rules.Clear();
                    rules.Add(new TableEditorReferenceRule
                    {
                        TargetTableKey = "affect",
                        ValueKind = TableEditorReferenceValueKind.Uid,
                        IsEnabledForRow = _ => true,
                    });
                }
                else if (string.Equals(headerName, "ApplyAffectUid", StringComparison.OrdinalIgnoreCase))
                {
                    rules.Clear();
                    rules.Add(new TableEditorReferenceRule
                    {
                        TargetTableKey = "affect",
                        ValueKind = TableEditorReferenceValueKind.Uid,
                        IsEnabledForRow = row => string.Equals(row != null && row.Values.TryGetValue("Kind", out string kind) ? kind : string.Empty, "ApplyAffectToTarget", StringComparison.OrdinalIgnoreCase),
                    });
                }
            }

            return rules;
        }

        private static TableEditorReferenceRule CreateConditionalStringIdRule(string tableKey, string kindValue)
        {
            return new TableEditorReferenceRule
            {
                TargetTableKey = tableKey,
                ValueKind = TableEditorReferenceValueKind.StringId,
                IsEnabledForRow = row => string.Equals(row != null && row.Values.TryGetValue("Kind", out string kind) ? kind : string.Empty, kindValue, StringComparison.OrdinalIgnoreCase),
            };
        }

        private static void EnsureModules()
        {
            if (_modules != null)
                return;

            _modules = new List<ITableEditorModule>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<ITableEditorModule>())
            {
                if (type == null || type.IsAbstract || type.IsInterface)
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

            _modules = _modules
                .GroupBy(static m => m.GetType().FullName, StringComparer.Ordinal)
                .Select(static g => g.First())
                .OrderBy(static m => m.PackageName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static m => m.ModuleName, StringComparer.OrdinalIgnoreCase)
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
