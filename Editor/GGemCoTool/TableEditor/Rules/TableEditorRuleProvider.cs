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
        private const string KindStat = "Stat";
        private const string KindDamage = "Damage";
        private const string KindElementDamage = "ElementDamage";
        private const string KindState = "State";
        private const string KindCrowdControl = "CrowdControl";
        private const string KindApplyAffectToTarget = "ApplyAffectToTarget";
        private const string KindFormulaVariable = "FormulaVariable";
        private const string KindHeal = "Heal";
        private const string KindCustom = "Custom";

        private static readonly string[] CommonColumns = { "AffectUid", "ModifierId", "Phase", "Kind" };
        private static readonly string[] StatColumns = { "StatId", "StatValue", "StatValueType", "StatOperation" };
        private static readonly string[] DamageColumns =
        {
            "DamageTypeId",
            "DamageBaseValue",
            "ScalingStatId",
            "ScalingCoefficient",
            "CanCrit",
            "IsDot",
            "SuppressDamageReaction",
            "ShowHitEffect"
        };
        private static readonly string[] StateColumns = { "StateId", "StateChance", "StateDurationOverride" };
        private static readonly string[] CrowdControlColumns = { "CrowdControlUid" };
        private static readonly string[] ApplyAffectColumns = { "ApplyAffectUid", "ApplyAffectChance", "ApplyAffectDurationOverride", "ConsumeOnProc" };
        private static readonly string[] FormulaVariableColumns = { "FormulaVariableId", "FormulaVariableValue", "FormulaVariableValueType", "FormulaVariableOperation" };
        private static readonly string[] HealColumns = { "HealBaseValue", "HealScalingStatId", "HealScalingCoefficient" };

        private static readonly string[] AllowedKinds =
        {
            KindStat,
            KindDamage,
            KindElementDamage,
            KindHeal,
            KindState,
            KindCrowdControl,
            KindApplyAffectToTarget,
            KindFormulaVariable,
            KindCustom,
        };

        private static readonly string[] AllKindSpecificColumns = StatColumns
            .Concat(DamageColumns)
            .Concat(HealColumns)
            .Concat(StateColumns)
            .Concat(CrowdControlColumns)
            .Concat(ApplyAffectColumns)
            .Concat(FormulaVariableColumns)
            .ToArray();

        private readonly List<TableEditorColumnRule> _rules;

        /// <summary>
        /// affect_modifier 테이블의 Kind별 Inspector 표시 규칙을 초기화합니다.
        /// 공통 컬럼은 항상 표시하고, Kind 전용 컬럼은 선택된 Kind와 일치할 때만 표시합니다.
        /// </summary>
        public AffectModifierTableRuleProvider()
        {
            _rules = new List<TableEditorColumnRule>();
            AddRules(CommonColumns, "Common", null, false, null, TableEditorInactiveDisplayMode.ShowDisabled);
            AddRules(StatColumns, "Stat Modifier", row => IsKind(row, KindStat), true, "Kind가 Stat일 때 필수 입력입니다.", TableEditorInactiveDisplayMode.Hide);
            AddRules(DamageColumns, "Damage Modifier", IsDamageKind, true, "Kind가 Damage 또는 ElementDamage일 때 필수 입력입니다.", TableEditorInactiveDisplayMode.Hide);
            AddRules(HealColumns, "Heal Modifier", row => IsKind(row, KindHeal), true, "Kind가 Heal일 때 필수 입력입니다.", TableEditorInactiveDisplayMode.Hide);
            AddRules(StateColumns, "State Modifier", row => IsKind(row, KindState), true, "Kind가 State일 때 필수 입력입니다.", TableEditorInactiveDisplayMode.Hide);
            AddRules(CrowdControlColumns, "Crowd Control Modifier", row => IsKind(row, KindCrowdControl), true, "Kind가 CrowdControl일 때 필수 입력입니다.", TableEditorInactiveDisplayMode.Hide);
            AddRules(ApplyAffectColumns, "Apply Affect Modifier", row => IsKind(row, KindApplyAffectToTarget), true, "Kind가 ApplyAffectToTarget일 때 필수 입력입니다.", TableEditorInactiveDisplayMode.Hide);
            AddRules(FormulaVariableColumns, "Formula Variable Modifier", row => IsKind(row, KindFormulaVariable), true, "Kind가 FormulaVariable일 때 필수 입력입니다.", TableEditorInactiveDisplayMode.Hide);
        }

        public bool CanHandle(TableEditorTableDefinition definition)
        {
            return definition != null && string.Equals(definition.TableKey, TableKey, StringComparison.OrdinalIgnoreCase);
        }

        public IReadOnlyList<TableEditorColumnRule> GetColumnRules() => _rules;

        /// <summary>
        /// Kind 변경 직전에 이전 Kind 전용 컬럼 값을 기본값으로 정리합니다.
        /// 숨겨진 컬럼의 과거 값이 저장 파일에 남아 런타임 해석을 오염시키는 것을 방지합니다.
        /// </summary>
        /// <param name="document">편집 중인 테이블 문서입니다.</param>
        /// <param name="row">값이 변경되는 행입니다.</param>
        /// <param name="changedColumnName">변경되는 컬럼 이름입니다.</param>
        /// <param name="nextValue">새로 설정할 원본 문자열 값입니다.</param>
        /// <returns>Kind 변경으로 후처리를 수행했으면 true입니다.</returns>
        public bool OnBeforeCellValueChanged(TableEditorDocument document, TableEditorDocumentRow row, string changedColumnName, string nextValue)
        {
            if (document == null || row == null)
                return false;

            if (!string.Equals(changedColumnName, "Kind", StringComparison.OrdinalIgnoreCase))
                return false;

            ClearColumns(document, row, StatColumns, string.Equals(nextValue, KindStat, StringComparison.OrdinalIgnoreCase));
            ClearColumns(document, row, DamageColumns, IsDamageKind(nextValue));
            ClearColumns(document, row, HealColumns, string.Equals(nextValue, KindHeal, StringComparison.OrdinalIgnoreCase));
            ClearColumns(document, row, StateColumns, string.Equals(nextValue, KindState, StringComparison.OrdinalIgnoreCase));
            ClearColumns(document, row, CrowdControlColumns, string.Equals(nextValue, KindCrowdControl, StringComparison.OrdinalIgnoreCase));
            ClearColumns(document, row, ApplyAffectColumns, string.Equals(nextValue, KindApplyAffectToTarget, StringComparison.OrdinalIgnoreCase));
            ClearColumns(document, row, FormulaVariableColumns, string.Equals(nextValue, KindFormulaVariable, StringComparison.OrdinalIgnoreCase));
            return true;
        }

        /// <summary>
        /// affect_modifier 행의 공통 컬럼과 Kind별 필수 컬럼을 검증합니다.
        /// 비활성 컬럼은 Inspector에서 숨기지만, 값이 남아 있으면 정보 메시지로 정리 필요성을 알려줍니다.
        /// </summary>
        /// <param name="definition">검증 대상 테이블 정의입니다.</param>
        /// <param name="row">검증할 테이블 행입니다.</param>
        /// <param name="columnMap">컬럼 이름과 정의 매핑입니다.</param>
        /// <param name="messages">검증 메시지를 추가할 컬렉션입니다.</param>
        public void ValidateRow(TableEditorTableDefinition definition, TableEditorDocumentRow row, IReadOnlyDictionary<string, TableEditorColumnDefinition> columnMap, List<TableEditorValidationMessage> messages)
        {
            if (row == null || messages == null)
                return;

            ValidateReference(row, columnMap, messages, "AffectUid", true, "AffectUid는 affect 테이블 Uid를 참조해야 합니다.");
            ValidateRequiredRaw(row, messages, "ModifierId", "ModifierId는 필수입니다.");
            ValidateAllowedValue(row, messages, "Phase", new[] { "OnApply", "OnTick", "OnExpire", "OnHit" }, "Phase 값이 유효하지 않습니다.");
            ValidateAllowedValue(row, messages, "Kind", AllowedKinds, "Kind 값이 유효하지 않습니다.");

            if (IsKind(row, KindStat))
            {
                ValidateReference(row, columnMap, messages, "StatId", true, "StatId는 stat.ID를 참조해야 합니다.");
                ValidateRequiredRaw(row, messages, "StatValueType", "StatValueType은 필수입니다.");
                ValidateRequiredRaw(row, messages, "StatOperation", "StatOperation은 필수입니다.");
                ValidateInactiveColumnsEmpty(row, messages, GetInactiveColumns(StatColumns), KindStat);
            }
            else if (IsDamageKind(row))
            {
                ValidateReference(row, columnMap, messages, "DamageTypeId", true, "DamageTypeId는 damage_type.ID를 참조해야 합니다.");
                ValidateOptionalReference(row, columnMap, messages, "ScalingStatId", "ScalingStatId는 stat.ID를 참조해야 합니다.");
                ValidateBooleanYN(row, messages, "CanCrit");
                ValidateBooleanYN(row, messages, "IsDot");
                ValidateBooleanYN(row, messages, "SuppressDamageReaction");
                ValidateBooleanYN(row, messages, "ShowHitEffect");
                ValidateInactiveColumnsEmpty(row, messages, GetInactiveColumns(DamageColumns), GetRaw(row, "Kind"));
            }
            else if (IsKind(row, KindHeal))
            {
                ValidateRange01(row, messages, "HealBaseValue");
                ValidateOptionalReference(row, columnMap, messages, "HealScalingStatId", "HealScalingStatId는 stat.ID를 참조해야 합니다.");
                ValidateRange01(row, messages, "HealScalingCoefficient");
                ValidateInactiveColumnsEmpty(row, messages, GetInactiveColumns(HealColumns), KindHeal);
            }
            else if (IsKind(row, KindState))
            {
                ValidateReference(row, columnMap, messages, "StateId", true, "StateId는 state.ID를 참조해야 합니다.");
                ValidateRange01(row, messages, "StateChance");
                ValidateNonNegative(row, messages, "StateDurationOverride");
                ValidateInactiveColumnsEmpty(row, messages, GetInactiveColumns(StateColumns), KindState);
            }
            else if (IsKind(row, KindCrowdControl))
            {
                ValidateReference(row, columnMap, messages, "CrowdControlUid", true, "CrowdControlUid는 crowd_control 테이블 Uid를 참조해야 합니다.");
                ValidateInactiveColumnsEmpty(row, messages, GetInactiveColumns(CrowdControlColumns), KindCrowdControl);
            }
            else if (IsKind(row, KindApplyAffectToTarget))
            {
                ValidateReference(row, columnMap, messages, "ApplyAffectUid", true, "ApplyAffectUid는 affect 테이블 Uid를 참조해야 합니다.");
                ValidateRange01(row, messages, "ApplyAffectChance");
                ValidateNonNegative(row, messages, "ApplyAffectDurationOverride");
                ValidateBooleanYN(row, messages, "ConsumeOnProc");
                ValidateInactiveColumnsEmpty(row, messages, GetInactiveColumns(ApplyAffectColumns), KindApplyAffectToTarget);
            }
            else if (IsKind(row, KindFormulaVariable))
            {
                ValidateRequiredRaw(row, messages, "FormulaVariableId", "FormulaVariableId는 필수입니다.");
                ValidateRequiredRaw(row, messages, "FormulaVariableValueType", "FormulaVariableValueType은 필수입니다.");
                ValidateRequiredRaw(row, messages, "FormulaVariableOperation", "FormulaVariableOperation은 필수입니다.");
                ValidateInactiveColumnsEmpty(row, messages, GetInactiveColumns(FormulaVariableColumns), KindFormulaVariable);
            }
        }

        /// <summary>
        /// affect_modifier 컬럼 규칙을 생성합니다.
        /// Kind별 전용 컬럼은 비활성 상태에서 숨겨 Inspector에 필요한 입력 그룹만 표시되도록 합니다.
        /// </summary>
        /// <param name="columnNames">규칙을 적용할 컬럼 이름 목록입니다.</param>
        /// <param name="sectionName">Inspector에 표시할 섹션 이름입니다.</param>
        /// <param name="isActive">현재 행에서 컬럼 그룹이 활성화되는지 판단하는 조건입니다.</param>
        /// <param name="isRequiredWhenActive">활성화 상태에서 필수 입력으로 볼지 여부입니다.</param>
        /// <param name="requiredMessage">필수 입력 누락 시 표시할 메시지입니다.</param>
        /// <param name="inactiveDisplayMode">비활성 상태의 표시 정책입니다.</param>
        private void AddRules(
            IEnumerable<string> columnNames,
            string sectionName,
            Func<TableEditorDocumentRow, bool> isActive,
            bool isRequiredWhenActive,
            string requiredMessage,
            TableEditorInactiveDisplayMode inactiveDisplayMode)
        {
            foreach (string columnName in columnNames)
            {
                _rules.Add(new TableEditorColumnRule
                {
                    ColumnName = columnName,
                    SectionName = sectionName,
                    IsActiveForRow = isActive,
                    IsRequiredWhenActive = isRequiredWhenActive,
                    RequiredMessage = requiredMessage,
                    InactiveDisplayMode = inactiveDisplayMode,
                    InactiveHint = isActive == null || inactiveDisplayMode == TableEditorInactiveDisplayMode.Hide ? null : "Kind와 맞지 않아 비활성화됩니다.",
                });
            }
        }

        /// <summary>
        /// 행의 Kind 값이 기대하는 Modifier Kind와 같은지 확인합니다.
        /// </summary>
        /// <param name="row">검사할 테이블 행입니다.</param>
        /// <param name="expectedKind">비교할 Kind 이름입니다.</param>
        /// <returns>Kind가 같으면 true입니다.</returns>
        private static bool IsKind(TableEditorDocumentRow row, string expectedKind)
        {
            if (row == null || string.IsNullOrWhiteSpace(expectedKind))
                return false;

            return string.Equals(GetRaw(row, "Kind"), expectedKind, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 행의 Kind가 Damage 계열 Modifier인지 확인합니다.
        /// ElementDamage는 Damage와 동일한 컬럼 그룹을 사용합니다.
        /// </summary>
        /// <param name="row">검사할 테이블 행입니다.</param>
        /// <returns>Damage 또는 ElementDamage이면 true입니다.</returns>
        private static bool IsDamageKind(TableEditorDocumentRow row)
        {
            return row != null && IsDamageKind(GetRaw(row, "Kind"));
        }

        /// <summary>
        /// 문자열 Kind 값이 Damage 계열 Modifier인지 확인합니다.
        /// </summary>
        /// <param name="kindRaw">검사할 Kind 원본 문자열입니다.</param>
        /// <returns>Damage 또는 ElementDamage이면 true입니다.</returns>
        private static bool IsDamageKind(string kindRaw)
        {
            return string.Equals(kindRaw, KindDamage, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(kindRaw, KindElementDamage, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 특정 Kind 그룹을 제외한 나머지 Kind 전용 컬럼 목록을 반환합니다.
        /// 불필요한 컬럼에 값이 남아 있는지 검증할 때 사용합니다.
        /// </summary>
        /// <param name="activeColumns">현재 Kind에서 사용하는 컬럼 목록입니다.</param>
        /// <returns>현재 Kind가 사용하지 않는 컬럼 목록입니다.</returns>
        private static IEnumerable<string> GetInactiveColumns(IReadOnlyCollection<string> activeColumns)
        {
            return AllKindSpecificColumns.Where(columnName => !activeColumns.Contains(columnName));
        }

        /// <summary>
        /// 지정한 컬럼의 원본 문자열 값을 반환합니다.
        /// 값이 없으면 빈 문자열을 반환하여 검증 로직의 null 분기를 줄입니다.
        /// </summary>
        /// <param name="row">조회할 테이블 행입니다.</param>
        /// <param name="headerName">조회할 컬럼 이름입니다.</param>
        /// <returns>컬럼 원본 문자열 값입니다.</returns>
        private static string GetRaw(TableEditorDocumentRow row, string headerName)
        {
            return row != null && row.Values.TryGetValue(headerName, out string value) ? value ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// 현재 Kind에서 사용하지 않는 컬럼 그룹의 값을 기본값으로 초기화합니다.
        /// </summary>
        /// <param name="document">편집 중인 테이블 문서입니다.</param>
        /// <param name="row">정리할 행입니다.</param>
        /// <param name="columnNames">정리 대상 컬럼 목록입니다.</param>
        /// <param name="keepCurrentGroup">현재 Kind에서 사용하는 그룹이면 true입니다.</param>
        private static void ClearColumns(TableEditorDocument document, TableEditorDocumentRow row, IEnumerable<string> columnNames, bool keepCurrentGroup)
        {
            if (keepCurrentGroup)
                return;

            foreach (string columnName in columnNames)
            {
                if (!row.Values.TryGetValue(columnName, out string currentRaw))
                    continue;

                string defaultRaw = GetDefaultRawForColumn(columnName);
                if (!string.Equals(currentRaw ?? string.Empty, defaultRaw, StringComparison.Ordinal))
                    document.SetCellValue(row, columnName, defaultRaw);
            }
        }

        /// <summary>
        /// 숨김 처리되는 컬럼을 정리할 때 사용할 기본 원본 문자열 값을 반환합니다.
        /// 숫자 컬럼은 0, 참조/문자열/Enum 컬럼은 공란으로 되돌립니다.
        /// </summary>
        /// <param name="columnName">기본값을 구할 컬럼 이름입니다.</param>
        /// <returns>테이블에 저장할 기본 원본 문자열입니다.</returns>
        private static string GetDefaultRawForColumn(string columnName)
        {
            switch (columnName)
            {
                case "CanCrit":
                case "IsDot":
                case "ConsumeOnProc":
                case "DamageBaseValue":
                case "ScalingCoefficient":
                case "HealBaseValue":
                case "HealScalingCoefficient":
                case "StateChance":
                case "StateDurationOverride":
                case "ApplyAffectChance":
                case "ApplyAffectDurationOverride":
                case "FormulaVariableValue":
                case "StatValue":
                    return "0";
                default:
                    return string.Empty;
            }
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

        private static void ValidateOptionalReference(TableEditorDocumentRow row, IReadOnlyDictionary<string, TableEditorColumnDefinition> columnMap, List<TableEditorValidationMessage> messages, string headerName, string missingMessage)
        {
            if (string.IsNullOrWhiteSpace(GetRaw(row, headerName)))
                return;

            ValidateReference(row, columnMap, messages, headerName, false, missingMessage);
        }

        private static void ValidateInactiveColumnsEmpty(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages, IEnumerable<string> columnNames, string kind)
        {
            foreach (string columnName in columnNames)
            {
                string raw = GetRaw(row, columnName);
                if (string.IsNullOrWhiteSpace(raw) || string.Equals(raw, "0", StringComparison.Ordinal))
                    continue;

                messages.Add(new TableEditorValidationMessage
                {
                    Severity = TableEditorValidationSeverity.Info,
                    Message = $"Kind={kind} 에서는 {columnName} 컬럼을 공란/0으로 두는 것을 권장합니다.",
                    RowStableId = row.stableId,
                });
            }
        }

        private static void ValidateRange01(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages, string headerName)
        {
            string raw = GetRaw(row, headerName);
            if (string.IsNullOrWhiteSpace(raw))
                return;

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value >= 0f && value <= 1f)
                return;

            messages.Add(new TableEditorValidationMessage
            {
                Severity = TableEditorValidationSeverity.Warning,
                Message = $"{headerName} 는 0~1 범위여야 합니다.",
                RowStableId = row.stableId,
            });
        }

        private static void ValidateNonNegative(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages, string headerName)
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
        /// Y/N 형식의 불리언 테이블 값이 유효한지 검증합니다.
        /// </summary>
        /// <param name="row">검증할 테이블 행입니다.</param>
        /// <param name="messages">검증 메시지를 추가할 컬렉션입니다.</param>
        /// <param name="headerName">검증할 컬럼 이름입니다.</param>
        private static void ValidateBooleanYN(TableEditorDocumentRow row, List<TableEditorValidationMessage> messages, string headerName)
        {
            string raw = GetRaw(row, headerName);
            if (string.IsNullOrWhiteSpace(raw))
                return;

            if (string.Equals(raw, "Y", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(raw, "N", StringComparison.OrdinalIgnoreCase))
                return;

            messages.Add(new TableEditorValidationMessage
            {
                Severity = TableEditorValidationSeverity.Warning,
                Message = $"{headerName} 는 Y 또는 N 이어야 합니다.",
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

        /// <summary>
        /// Kind 변경 직전에 이전 Kind 전용 컬럼 값을 기본값으로 정리합니다.
        /// 숨겨진 컬럼의 과거 값이 저장 파일에 남아 런타임 해석을 오염시키는 것을 방지합니다.
        /// </summary>
        /// <param name="document">편집 중인 테이블 문서입니다.</param>
        /// <param name="row">값이 변경되는 행입니다.</param>
        /// <param name="changedColumnName">변경되는 컬럼 이름입니다.</param>
        /// <param name="nextValue">새로 설정할 원본 문자열 값입니다.</param>
        /// <returns>Kind 변경으로 후처리를 수행했으면 true입니다.</returns>
        public bool OnBeforeCellValueChanged(TableEditorDocument document, TableEditorDocumentRow row, string changedColumnName, string nextValue)
        {
            return false;
        }

        /// <summary>
        /// affect_modifier 행의 공통 컬럼과 Kind별 필수 컬럼을 검증합니다.
        /// 비활성 컬럼은 Inspector에서 숨기지만, 값이 남아 있으면 정보 메시지로 정리 필요성을 알려줍니다.
        /// </summary>
        /// <param name="definition">검증 대상 테이블 정의입니다.</param>
        /// <param name="row">검증할 테이블 행입니다.</param>
        /// <param name="columnMap">컬럼 이름과 정의 매핑입니다.</param>
        /// <param name="messages">검증 메시지를 추가할 컬렉션입니다.</param>
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

        /// <summary>
        /// affect_modifier 행의 공통 컬럼과 Kind별 필수 컬럼을 검증합니다.
        /// 비활성 컬럼은 Inspector에서 숨기지만, 값이 남아 있으면 정보 메시지로 정리 필요성을 알려줍니다.
        /// </summary>
        /// <param name="definition">검증 대상 테이블 정의입니다.</param>
        /// <param name="row">검증할 테이블 행입니다.</param>
        /// <param name="columnMap">컬럼 이름과 정의 매핑입니다.</param>
        /// <param name="messages">검증 메시지를 추가할 컬렉션입니다.</param>
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

        /// <summary>
        /// 지정한 컬럼의 원본 문자열 값을 반환합니다.
        /// 값이 없으면 빈 문자열을 반환하여 검증 로직의 null 분기를 줄입니다.
        /// </summary>
        /// <param name="row">조회할 테이블 행입니다.</param>
        /// <param name="headerName">조회할 컬럼 이름입니다.</param>
        /// <returns>컬럼 원본 문자열 값입니다.</returns>
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
