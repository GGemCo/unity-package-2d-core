using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    public enum TableEditorReferenceValueKind
    {
        None = 0,
        Uid = 1,
        StringId = 2,
    }

    internal enum TableEditorInactiveDisplayMode
    {
        ShowDisabled = 0,
        Hide = 1,
        ReadOnly = 2,
    }

    public sealed class TableEditorReferenceRule
    {
        public string TargetTableKey;
        public TableEditorReferenceValueKind ValueKind;
        public Func<TableEditorDocumentRow, bool> IsEnabledForRow;
        public string EmptyMessage;

        public bool IsEnabled(TableEditorDocumentRow row)
        {
            return IsEnabledForRow == null || IsEnabledForRow(row);
        }
    }

    internal sealed class TableEditorColumnRule
    {
        public string ColumnName;
        public string SectionName;
        public Func<TableEditorDocumentRow, bool> IsActiveForRow;
        public bool IsRequiredWhenActive;
        public string RequiredMessage;
        public TableEditorInactiveDisplayMode InactiveDisplayMode = TableEditorInactiveDisplayMode.ShowDisabled;
        public string InactiveHint;

        public bool IsActive(TableEditorDocumentRow row)
        {
            return IsActiveForRow == null || IsActiveForRow(row);
        }
    }

    internal interface ITableEditorTableRuleProvider
    {
        bool CanHandle(TableEditorTableDefinition definition);
        IReadOnlyList<TableEditorColumnRule> GetColumnRules();
        bool OnBeforeCellValueChanged(TableEditorDocument document, TableEditorDocumentRow row, string changedColumnName, string nextValue);
        void ValidateRow(TableEditorTableDefinition definition, TableEditorDocumentRow row, IReadOnlyDictionary<string, TableEditorColumnDefinition> columnMap, List<TableEditorValidationMessage> messages);
    }

    internal static class TableEditorRuleProviderRegistry
    {
        private static readonly ITableEditorTableRuleProvider[] Providers =
        {
            new AffectModifierTableRuleProvider(),
            new CrowdControlCommonTableRuleProvider(),
            new CrowdControlKnockBackTableRuleProvider(),
            new CrowdControlKnockDownTableRuleProvider(),
            new CrowdControlKnockUpTableRuleProvider(),
        };

        public static ITableEditorTableRuleProvider GetProvider(TableEditorTableDefinition definition)
        {
            if (definition == null)
                return null;

            for (int i = 0; i < Providers.Length; i++)
            {
                if (Providers[i] != null && Providers[i].CanHandle(definition))
                    return Providers[i];
            }

            return null;
        }
    }

    internal sealed class AffectModifierTableRuleProvider : ITableEditorTableRuleProvider
    {
        private const string TableKey = "affect_modifier";
        private static readonly string[] CommonColumns = { "ModifierId", "AffectUid", "Phase", "Kind", "ConditionId" };
        private static readonly string[] AllowedKinds =
        {
            "Stat",
            "Damage",
            "ElementDamage",
            "Heal",
            "State",
            "CrowdControl",
            "ApplyAffectToTarget",
            "FormulaVariable",
            "Custom"
        };

        private readonly List<TableEditorColumnRule> _rules;

        /// <summary>
        /// affect_modifier 공통 메타 테이블의 표시 규칙을 구성합니다.
        /// </summary>
        /// <remarks>
        /// Kind별 상세 값은 affect_modifier_* 상세 테이블에서 편집하므로,
        /// 이 규칙 제공자는 ModifierId/AffectUid/Phase/Kind/ConditionId 같은 공통 컬럼만 검증합니다.
        /// </remarks>
        public AffectModifierTableRuleProvider()
        {
            _rules = new List<TableEditorColumnRule>();
            AddRules(CommonColumns, "Common");
        }

        public bool CanHandle(TableEditorTableDefinition definition)
        {
            return definition != null && string.Equals(definition.TableKey, TableKey, StringComparison.OrdinalIgnoreCase);
        }

        public IReadOnlyList<TableEditorColumnRule> GetColumnRules() => _rules;

        public bool OnBeforeCellValueChanged(TableEditorDocument document, TableEditorDocumentRow row, string changedColumnName, string nextValue)
        {
            return false;
        }

        /// <summary>
        /// affect_modifier 공통 메타 Row의 기본 정합성을 검증합니다.
        /// </summary>
        /// <param name="definition">검증 대상 테이블 정의입니다.</param>
        /// <param name="row">검증 대상 Row입니다.</param>
        /// <param name="columnMap">컬럼 정의 맵입니다.</param>
        /// <param name="messages">검증 메시지를 추가할 목록입니다.</param>
        public void ValidateRow(TableEditorTableDefinition definition, TableEditorDocumentRow row, IReadOnlyDictionary<string, TableEditorColumnDefinition> columnMap, List<TableEditorValidationMessage> messages)
        {
            if (row == null || messages == null)
                return;

            ValidateRequiredRaw(row, messages, "ModifierId", "ModifierId는 필수입니다.");
            ValidateReference(row, columnMap, messages, "AffectUid", true, "AffectUid는 affect 테이블 Uid를 참조해야 합니다.");
            ValidateAllowedValue(row, messages, "Phase", new[] { "OnApply", "OnTick", "OnExpire", "OnHit" }, "Phase 값이 유효하지 않습니다.");
            ValidateAllowedValue(row, messages, "Kind", AllowedKinds, "Kind 값이 유효하지 않습니다.");
        }

        /// <summary>
        /// 공통 메타 컬럼 표시 규칙을 등록합니다.
        /// </summary>
        /// <param name="columnNames">등록할 컬럼 이름 목록입니다.</param>
        /// <param name="sectionName">테이블 에디터에서 표시할 섹션 이름입니다.</param>
        private void AddRules(IEnumerable<string> columnNames, string sectionName)
        {
            foreach (string columnName in columnNames)
            {
                _rules.Add(new TableEditorColumnRule
                {
                    ColumnName = columnName,
                    SectionName = sectionName,
                    IsActiveForRow = _ => true,
                    InactiveDisplayMode = TableEditorInactiveDisplayMode.ShowDisabled,
                });
            }
        }

        private static string GetRaw(TableEditorDocumentRow row, string headerName)
        {
            return row != null && row.Values.TryGetValue(headerName, out string value) ? value ?? string.Empty : string.Empty;
        }

        private static void ValidateRequiredRaw(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages, string headerName, string message)
        {
            if (!string.IsNullOrWhiteSpace(GetRaw(row, headerName)))
                return;

            messages.Add(new TableEditorValidationMessage
            {
                Severity = TableEditorValidationSeverity.Warning,
                Message = message,
                RowStableId = row.stableId,
            });
        }

        private static void ValidateAllowedValue(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages, string headerName, IReadOnlyList<string> allowedValues, string message)
        {
            string raw = GetRaw(row, headerName);
            if (string.IsNullOrWhiteSpace(raw))
            {
                messages.Add(new TableEditorValidationMessage
                {
                    Severity = TableEditorValidationSeverity.Warning,
                    Message = message,
                    RowStableId = row.stableId,
                });
                return;
            }

            for (int i = 0; i < allowedValues.Count; i++)
            {
                if (string.Equals(raw, allowedValues[i], StringComparison.OrdinalIgnoreCase))
                    return;
            }

            messages.Add(new TableEditorValidationMessage
            {
                Severity = TableEditorValidationSeverity.Warning,
                Message = $"{message} ({headerName}={raw})",
                RowStableId = row.stableId,
            });
        }

        private static void ValidateReference(TableEditorDocumentRow row, IReadOnlyDictionary<string, TableEditorColumnDefinition> columnMap, List<TableEditorValidationMessage> messages, string headerName, bool required, string missingMessage)
        {
            string raw = GetRaw(row, headerName);
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (required)
                {
                    messages.Add(new TableEditorValidationMessage
                    {
                        Severity = TableEditorValidationSeverity.Warning,
                        Message = missingMessage,
                        RowStableId = row.stableId,
                    });
                }
                return;
            }

            if (!columnMap.TryGetValue(headerName, out TableEditorColumnDefinition column))
                return;

            TableEditorReferenceRule rule = column.ResolveReferenceRule(row);
            if (rule == null)
                return;

            bool exists = rule.ValueKind == TableEditorReferenceValueKind.Uid
                ? int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int uid) && uid > 0 && TableEditorReferenceCache.Contains(column.GetReferenceTable(rule), uid)
                : TableEditorReferenceCache.Contains(column.GetReferenceTable(rule), raw);
            if (exists)
                return;

            messages.Add(new TableEditorValidationMessage
            {
                Severity = TableEditorValidationSeverity.Warning,
                Message = missingMessage,
                RowStableId = row.stableId,
            });
        }
    }


    internal abstract class CrowdControlDetailTableRuleProviderBase : ITableEditorTableRuleProvider
    {
        private readonly string _tableKey;
        private readonly CrowdControlConstants.Type _expectedType;
        private readonly List<TableEditorColumnRule> _rules;

        protected CrowdControlDetailTableRuleProviderBase(string tableKey, CrowdControlConstants.Type expectedType, IEnumerable<string> motionColumns)
        {
            _tableKey = tableKey;
            _expectedType = expectedType;
            _rules = new List<TableEditorColumnRule>();

            AddRules(new[] { "CrowdControlUid" }, "Reference");
            AddRules(motionColumns, "Motion / Timing");
            AddRules(new[] { "EndYMode", "EndYOffset", "EndYAbsolute" }, "End Position");
            AddRules(new[] { "RecoverTime" }, "Recover");
            AddRules(new[] { "IsStopOnWall", "IsGroundOnly", "IsAirOnly" }, "Flags");
        }

        public bool CanHandle(TableEditorTableDefinition definition)
        {
            return definition != null && string.Equals(definition.TableKey, _tableKey, StringComparison.OrdinalIgnoreCase);
        }

        public IReadOnlyList<TableEditorColumnRule> GetColumnRules() => _rules;

        public bool OnBeforeCellValueChanged(TableEditorDocument document, TableEditorDocumentRow row, string changedColumnName, string nextValue)
        {
            return false;
        }

        public void ValidateRow(TableEditorTableDefinition definition, TableEditorDocumentRow row, IReadOnlyDictionary<string, TableEditorColumnDefinition> columnMap, List<TableEditorValidationMessage> messages)
        {
            if (row == null || messages == null)
                return;

            int crowdControlUid = ParsePositiveInt(GetRaw(row, "CrowdControlUid"));
            if (crowdControlUid <= 0)
            {
                messages.Add(new TableEditorValidationMessage
                {
                    Severity = TableEditorValidationSeverity.Warning,
                    Message = "CrowdControlUid는 필수입니다.",
                    RowStableId = row.stableId,
                });
                return;
            }

            TableCrowdControl table = TableLoaderManager.LoadCrowdControlTable(forceReload: false);
            StruckTableCrowdControl crowdControlRow = table != null ? table.GetDataByUid(crowdControlUid) : null;
            if (crowdControlRow == null)
            {
                messages.Add(new TableEditorValidationMessage
                {
                    Severity = TableEditorValidationSeverity.Warning,
                    Message = $"CrowdControlUid 참조를 찾을 수 없습니다: {crowdControlUid}",
                    RowStableId = row.stableId,
                });
                return;
            }

            if (crowdControlRow.Type != _expectedType)
            {
                messages.Add(new TableEditorValidationMessage
                {
                    Severity = TableEditorValidationSeverity.Warning,
                    Message = $"참조한 crowd_control 타입이 {_expectedType} 이(가) 아닙니다. 현재 타입: {crowdControlRow.Type}",
                    RowStableId = row.stableId,
                });
            }

            ValidateCoreValues(row, messages);

            bool isGroundOnly = ParseBool01(GetRaw(row, "IsGroundOnly"));
            bool isAirOnly = ParseBool01(GetRaw(row, "IsAirOnly"));
            if (isGroundOnly && isAirOnly)
            {
                messages.Add(new TableEditorValidationMessage
                {
                    Severity = TableEditorValidationSeverity.Warning,
                    Message = "IsGroundOnly 와 IsAirOnly 를 동시에 활성화하지 않는 것이 좋습니다.",
                    RowStableId = row.stableId,
                });
            }
        }

        protected virtual void ValidateCoreValues(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages)
        {
            ValidateNonNegative(row, messages, "RecoverTime");
            ValidateNumeric(row, messages, "EndYOffset");
            ValidateNumeric(row, messages, "EndYAbsolute");
        }

        private void AddRules(IEnumerable<string> columnNames, string sectionName)
        {
            foreach (string columnName in columnNames)
            {
                _rules.Add(new TableEditorColumnRule
                {
                    ColumnName = columnName,
                    SectionName = sectionName,
                    IsActiveForRow = _ => true,
                    InactiveDisplayMode = TableEditorInactiveDisplayMode.ShowDisabled,
                });
            }
        }

        protected static string GetRaw(TableEditorDocumentRow row, string headerName)
        {
            return row != null && row.Values.TryGetValue(headerName, out string value) ? value ?? string.Empty : string.Empty;
        }

        protected static int ParsePositiveInt(string raw)
        {
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value > 0 ? value : 0;
        }

        protected static bool ParseBool01(string raw)
        {
            return string.Equals(raw, "1", StringComparison.Ordinal)
                || string.Equals(raw, "Y", StringComparison.OrdinalIgnoreCase)
                || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
        }

        protected static void ValidateNonNegative(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages, string headerName)
        {
            string raw = GetRaw(row, headerName);
            if (string.IsNullOrWhiteSpace(raw))
                return;

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value >= 0f)
                return;

            messages.Add(new TableEditorValidationMessage
            {
                Severity = TableEditorValidationSeverity.Warning,
                Message = $"{headerName} 는 0 이상이어야 합니다.",
                RowStableId = row.stableId,
            });
        }

        protected static void ValidateNumeric(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages, string headerName)
        {
            string raw = GetRaw(row, headerName);
            if (string.IsNullOrWhiteSpace(raw))
                return;

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return;

            messages.Add(new TableEditorValidationMessage
            {
                Severity = TableEditorValidationSeverity.Warning,
                Message = $"{headerName} 값이 숫자가 아닙니다.",
                RowStableId = row.stableId,
            });
        }
    }

    internal sealed class CrowdControlCommonTableRuleProvider : ITableEditorTableRuleProvider
    {
        private const string TableKey = "crowd_control";
        private readonly List<TableEditorColumnRule> _rules;

        public CrowdControlCommonTableRuleProvider()
        {
            _rules = new List<TableEditorColumnRule>();
            AddRuleGroup(new[] { "Uid", "Name", "Type" }, "Common");
            AddRuleGroup(new[] { "DirectionType", "FixedDirectionX", "FixedDirectionY", "Distance", "EaseType", "Duration" }, "Motion");
            AddRuleGroup(new[] { "EndViewportPolicy", "EndViewportClampAxis", "EndViewportPadding" }, "End Viewport");
            AddRuleGroup(new[] { "IsUseKnockbackStatus", "IsUseDontControlStatus", "StaggerAnimationName" }, "State / Animation");
        }

        public bool CanHandle(TableEditorTableDefinition definition)
        {
            return definition != null && string.Equals(definition.TableKey, TableKey, StringComparison.OrdinalIgnoreCase);
        }

        public IReadOnlyList<TableEditorColumnRule> GetColumnRules() => _rules;
        public bool OnBeforeCellValueChanged(TableEditorDocument document, TableEditorDocumentRow row, string changedColumnName, string nextValue) => false;

        public void ValidateRow(TableEditorTableDefinition definition, TableEditorDocumentRow row, IReadOnlyDictionary<string, TableEditorColumnDefinition> columnMap, List<TableEditorValidationMessage> messages)
        {
            if (row == null || messages == null)
                return;

            string typeRaw = GetRaw(row, "Type");
            if (!Enum.TryParse(typeRaw, true, out CrowdControlConstants.Type crowdControlType) || crowdControlType == CrowdControlConstants.Type.None)
            {
                messages.Add(new TableEditorValidationMessage
                {
                    Severity = TableEditorValidationSeverity.Warning,
                    Message = "Type 값을 확인해주세요.",
                    RowStableId = row.stableId,
                });
            }

            ValidatePositiveOrZero(row, messages, "Distance");
            ValidatePositiveOrZero(row, messages, "Duration");
            ValidatePositiveOrZero(row, messages, "EndViewportPadding");
            ValidateEnumValue<CrowdControlConstants.EndViewportPolicy>(row, messages, "EndViewportPolicy");
            ValidateEnumValue<CrowdControlConstants.EndViewportClampAxis>(row, messages, "EndViewportClampAxis");
        }

        private void AddRuleGroup(IEnumerable<string> columns, string section)
        {
            foreach (string column in columns)
            {
                _rules.Add(new TableEditorColumnRule
                {
                    ColumnName = column,
                    SectionName = section,
                    IsActiveForRow = _ => true,
                    InactiveDisplayMode = TableEditorInactiveDisplayMode.ShowDisabled,
                });
            }
        }

        private void AddLegacyRuleGroup(IEnumerable<string> columns, string section)
        {
            foreach (string column in columns)
            {
                _rules.Add(new TableEditorColumnRule
                {
                    ColumnName = column,
                    SectionName = section,
                    IsActiveForRow = _ => false,
                    InactiveDisplayMode = TableEditorInactiveDisplayMode.ReadOnly,
                    InactiveHint = "타입별 상세 테이블(crowd_control_knock_back / knock_down / knock_up)에서 편집하세요.",
                });
            }
        }

        private static string GetRaw(TableEditorDocumentRow row, string headerName)
        {
            return row != null && row.Values.TryGetValue(headerName, out string value) ? value ?? string.Empty : string.Empty;
        }

        private static void ValidatePositiveOrZero(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages, string headerName)
        {
            string raw = GetRaw(row, headerName);
            if (string.IsNullOrWhiteSpace(raw))
                return;

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value >= 0f)
                return;

            messages.Add(new TableEditorValidationMessage
            {
                Severity = TableEditorValidationSeverity.Warning,
                Message = $"{headerName} 는 0 이상이어야 합니다.",
                RowStableId = row.stableId,
            });
        }

        /// <summary>
        /// 지정한 테이블 컬럼 값이 대상 Enum으로 변환 가능한지 검사합니다.
        /// 컬럼이 없거나 값이 비어 있으면 기존 데이터 호환을 위해 검사를 생략합니다.
        /// </summary>
        /// <typeparam name="TEnum">검사할 Enum 타입입니다.</typeparam>
        /// <param name="row">검사할 테이블 행입니다.</param>
        /// <param name="messages">검증 메시지를 추가할 목록입니다.</param>
        /// <param name="headerName">검사할 컬럼 이름입니다.</param>
        private static void ValidateEnumValue<TEnum>(
            TableEditorDocumentRow row,
            List<TableEditorValidationMessage> messages,
            string headerName)
            where TEnum : struct, Enum
        {
            string raw = GetRaw(row, headerName);
            if (string.IsNullOrWhiteSpace(raw))
                return;

            if (Enum.TryParse(raw, true, out TEnum _))
                return;

            messages.Add(new TableEditorValidationMessage
            {
                Severity = TableEditorValidationSeverity.Warning,
                Message = $"{headerName} 값을 확인해주세요.",
                RowStableId = row.stableId,
            });
        }
    }

    internal sealed class CrowdControlKnockBackTableRuleProvider : CrowdControlDetailTableRuleProviderBase
    {
        public CrowdControlKnockBackTableRuleProvider()
            : base("crowd_control_knock_back", CrowdControlConstants.Type.KnockBack, new[] { "DownWaitTime" })
        {
        }

        protected override void ValidateCoreValues(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages)
        {
            base.ValidateCoreValues(row, messages);
            ValidateNonNegative(row, messages, "DownWaitTime");
        }
    }

    internal sealed class CrowdControlKnockDownTableRuleProvider : CrowdControlDetailTableRuleProviderBase
    {
        public CrowdControlKnockDownTableRuleProvider()
            : base("crowd_control_knock_down", CrowdControlConstants.Type.KnockDown, new[] { "DownWaitTime" })
        {
        }

        protected override void ValidateCoreValues(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages)
        {
            base.ValidateCoreValues(row, messages);
            ValidateNonNegative(row, messages, "DownWaitTime");
        }
    }

    internal sealed class CrowdControlKnockUpTableRuleProvider : CrowdControlDetailTableRuleProviderBase
    {
        public CrowdControlKnockUpTableRuleProvider()
            : base("crowd_control_knock_up", CrowdControlConstants.Type.KnockUp, new[]
            {
                "Height",
                "RiseTime",
                "AirTime",
                "FallTime",
                "RiseAnimationName",
                "AirAnimationName",
                "FallAnimationName",
                "RiseEaseType",
                "FallEaseType"
            })
        {
        }

        protected override void ValidateCoreValues(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages)
        {
            base.ValidateCoreValues(row, messages);
            ValidateNonNegative(row, messages, "Height");
            ValidateNonNegative(row, messages, "RiseTime");
            ValidateNonNegative(row, messages, "AirTime");
            ValidateNonNegative(row, messages, "FallTime");
        }
    }

}
