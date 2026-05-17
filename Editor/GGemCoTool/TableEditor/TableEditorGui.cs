using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GGemCo2DCoreEditor
{
    internal static class TableEditorGui
    {
        public static VisualElement BuildInspector(
            EditorWindow owner,
            TableEditorTableDefinition currentDefinition,
            IReadOnlyList<TableEditorColumnDefinition> columns,
            TableEditorDocumentRow row,
            Action<string, string> onValueChanged,
            Action<TableEditorTableDefinition, int> onJumpToReference)
        {
            ScrollView root = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1f,
                    paddingLeft = 6f,
                    paddingRight = 6f,
                    paddingTop = 6f,
                    paddingBottom = 6f,
                }
            };

            if (row == null)
            {
                root.Add(new HelpBox("행을 선택해주세요.", HelpBoxMessageType.Info));
                return root;
            }

            object previewObject = TableEditorValueUtility.BuildRowObject(currentDefinition, row, out List<string> fieldErrors);
            row.cachedDisplayName = TableEditorReflectionUtility.GetDisplayName(previewObject, 0);

            Label title = new Label(row.cachedDisplayName)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 6f,
                }
            };
            root.Add(title);

            if (fieldErrors.Count > 0)
            {
                for (int i = 0; i < fieldErrors.Count; i++)
                    root.Add(new HelpBox(fieldErrors[i], HelpBoxMessageType.Warning));
            }

            ITableEditorTableRuleProvider ruleProvider = TableEditorRuleProviderRegistry.GetProvider(currentDefinition);
            Dictionary<string, TableEditorColumnRule> ruleByColumn = BuildRuleMap(ruleProvider);
            HashSet<string> drawnColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string lastSectionName = null;

            for (int i = 0; i < columns.Count; i++)
            {
                TableEditorColumnDefinition column = columns[i];
                ruleByColumn.TryGetValue(column.HeaderName, out TableEditorColumnRule columnRule);
                if (columnRule != null && columnRule.InactiveDisplayMode == TableEditorInactiveDisplayMode.Hide && !columnRule.IsActive(row))
                    continue;

                string sectionName = columnRule?.SectionName;
                if (!string.Equals(lastSectionName, sectionName, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrWhiteSpace(sectionName))
                        root.Add(CreateSectionHeader(sectionName));
                    lastSectionName = sectionName;
                }

                root.Add(CreateField(owner, currentDefinition, column, row, columnRule, onValueChanged, onJumpToReference));
                drawnColumns.Add(column.HeaderName);
            }

            if (ruleProvider != null)
            {
                IReadOnlyList<TableEditorColumnRule> rules = ruleProvider.GetColumnRules();
                for (int i = 0; i < rules.Count; i++)
                {
                    TableEditorColumnRule rule = rules[i];
                    if (rule == null || drawnColumns.Contains(rule.ColumnName))
                        continue;
                    if (rule.InactiveDisplayMode == TableEditorInactiveDisplayMode.Hide && !rule.IsActive(row))
                        continue;

                    if (!string.Equals(lastSectionName, rule.SectionName, StringComparison.Ordinal))
                    {
                        if (!string.IsNullOrWhiteSpace(rule.SectionName))
                            root.Add(CreateSectionHeader(rule.SectionName));
                        lastSectionName = rule.SectionName;
                    }

                    root.Add(CreateMissingFieldNotice(rule, row));
                }
            }

            return root;
        }

        /// <summary>
        /// 검증 결과 패널을 생성합니다.
        /// 검증 결과가 오래된 상태이면 상세 HelpBox 목록을 만들지 않고 요약만 표시하여 셀 편집 중 UI 재생성 비용을 줄입니다.
        /// </summary>
        /// <param name="messages">표시할 검증 메시지 목록입니다.</param>
        /// <param name="selectedRowStableId">선택된 행의 안정 식별자입니다.</param>
        /// <param name="showOnlySelected">선택 행 메시지만 표시할지 여부입니다.</param>
        /// <param name="isStale">검증 결과가 현재 문서보다 오래되었는지 여부입니다.</param>
        /// <returns>검증 결과 UI 루트입니다.</returns>
        public static VisualElement BuildValidationView(IReadOnlyList<TableEditorValidationMessage> messages, int selectedRowStableId, bool showOnlySelected, bool isStale = false)
        {
            ScrollView root = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1f,
                    paddingLeft = 4f,
                    paddingRight = 4f,
                    paddingTop = 4f,
                    paddingBottom = 4f,
                }
            };

            if (messages == null || messages.Count == 0)
            {
                root.Add(new HelpBox("검증 결과가 없습니다. Validate 버튼으로 다시 확인하세요.", HelpBoxMessageType.Info));
                return root;
            }

            CountValidationMessages(messages, out int errorCount, out int warningCount);

            if (isStale)
            {
                root.Add(new HelpBox("데이터가 변경되어 기존 검증 결과가 오래되었습니다. Validate 버튼으로 다시 확인하세요.", HelpBoxMessageType.Warning));
                root.Add(new HelpBox($"이전 검증 결과 - Errors: {errorCount}, Warnings: {warningCount}", errorCount > 0 ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning));
                return root;
            }

            root.Add(new HelpBox($"Errors: {errorCount}, Warnings: {warningCount}", errorCount > 0 ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning));

            for (int i = 0; i < messages.Count; i++)
            {
                TableEditorValidationMessage message = messages[i];
                if (showOnlySelected && selectedRowStableId > 0 && message.RowStableId > 0 && message.RowStableId != selectedRowStableId)
                    continue;

                HelpBoxMessageType type = message.Severity == TableEditorValidationSeverity.Error
                    ? HelpBoxMessageType.Error
                    : (message.Severity == TableEditorValidationSeverity.Warning ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info);
                root.Add(new HelpBox(message.Message, type));
            }

            return root;
        }

        /// <summary>
        /// 검증 메시지 목록에서 오류와 경고 개수를 계산합니다.
        /// 상세 HelpBox를 만들지 않는 오래된 검증 결과 표시에서도 요약을 유지하기 위한 보조 함수입니다.
        /// </summary>
        /// <param name="messages">집계할 검증 메시지 목록입니다.</param>
        /// <param name="errorCount">오류 메시지 개수입니다.</param>
        /// <param name="warningCount">경고 메시지 개수입니다.</param>
        private static void CountValidationMessages(IReadOnlyList<TableEditorValidationMessage> messages, out int errorCount, out int warningCount)
        {
            errorCount = 0;
            warningCount = 0;
            if (messages == null)
                return;

            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Severity == TableEditorValidationSeverity.Error)
                    errorCount++;
                else if (messages[i].Severity == TableEditorValidationSeverity.Warning)
                    warningCount++;
            }
        }

        private static Dictionary<string, TableEditorColumnRule> BuildRuleMap(ITableEditorTableRuleProvider provider)
        {
            Dictionary<string, TableEditorColumnRule> result = new Dictionary<string, TableEditorColumnRule>(StringComparer.OrdinalIgnoreCase);
            if (provider == null)
                return result;

            IReadOnlyList<TableEditorColumnRule> rules = provider.GetColumnRules();
            for (int i = 0; i < rules.Count; i++)
            {
                TableEditorColumnRule rule = rules[i];
                if (rule != null && !string.IsNullOrWhiteSpace(rule.ColumnName))
                    result[rule.ColumnName] = rule;
            }

            return result;
        }

        private static VisualElement CreateSectionHeader(string sectionName)
        {
            Label header = new Label(sectionName)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 8f,
                    marginBottom = 4f,
                    fontSize = 12,
                }
            };
            return header;
        }

        private static VisualElement CreateMissingFieldNotice(TableEditorColumnRule rule, TableEditorDocumentRow row)
        {
            HelpBox box = new HelpBox($"{rule.ColumnName}: 현재 row 클래스/헤더에 없는 컬럼입니다.", HelpBoxMessageType.Info);
            bool isActive = rule == null || rule.IsActive(row);
            box.SetEnabled(isActive);
            return box;
        }

        private static VisualElement CreateField(
            EditorWindow owner,
            TableEditorTableDefinition currentDefinition,
            TableEditorColumnDefinition column,
            TableEditorDocumentRow row,
            TableEditorColumnRule columnRule,
            Action<string, string> onValueChanged,
            Action<TableEditorTableDefinition, int> onJumpToReference)
        {
            VisualElement container = new VisualElement
            {
                style =
                {
                    marginBottom = 6f,
                    paddingLeft = 6f,
                    paddingRight = 6f,
                    paddingTop = 6f,
                    paddingBottom = 6f,
                    borderBottomWidth = 1f,
                    borderTopWidth = 1f,
                    borderLeftWidth = 1f,
                    borderRightWidth = 1f,
                    borderBottomColor = new Color(0.23f, 0.23f, 0.23f),
                    borderTopColor = new Color(0.23f, 0.23f, 0.23f),
                    borderLeftColor = new Color(0.23f, 0.23f, 0.23f),
                    borderRightColor = new Color(0.23f, 0.23f, 0.23f),
                }
            };

            bool isActive = columnRule == null || columnRule.IsActive(row);
            Label label = new Label(column.HeaderName)
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4f }
            };
            container.Add(label);

            row.Values.TryGetValue(column.HeaderName, out string rawValue);

            VisualElement input = CreateInputField(owner, currentDefinition, column, row, rawValue ?? string.Empty, onValueChanged, onJumpToReference);
            if (input != null)
            {
                input.SetEnabled(isActive || (columnRule != null && columnRule.InactiveDisplayMode == TableEditorInactiveDisplayMode.ReadOnly));
                container.Add(input);
            }

            if (!isActive && !string.IsNullOrWhiteSpace(columnRule?.InactiveHint))
                container.Add(new HelpBox(columnRule.InactiveHint, HelpBoxMessageType.Info));

            if (!column.ExistsInRowType)
                container.Add(new HelpBox("현재 row 클래스에 없는 컬럼입니다. 저장은 유지되지만 타입 검증은 제한됩니다.", HelpBoxMessageType.Info));

            container.SetEnabled(isActive || columnRule == null || columnRule.InactiveDisplayMode != TableEditorInactiveDisplayMode.ShowDisabled ? true : false);
            return container;
        }

        private static VisualElement CreateInputField(
            EditorWindow owner,
            TableEditorTableDefinition currentDefinition,
            TableEditorColumnDefinition column,
            TableEditorDocumentRow row,
            string rawValue,
            Action<string, string> onValueChanged,
            Action<TableEditorTableDefinition, int> onJumpToReference)
        {
            Type type = column.MemberInfo != null ? column.ValueType : typeof(string);
            if (!TableEditorValueUtility.TryConvertFromRaw(rawValue, type, out object converted, out _))
                converted = GetDefaultValue(type);

            TableEditorReferenceRule referenceRule = column.ResolveReferenceRule(row);
            TableEditorTableDefinition referenceTable = column.GetReferenceTable(referenceRule);
            if (referenceRule != null)
            {
                if (referenceRule.ValueKind == TableEditorReferenceValueKind.StringId)
                    return CreateStringIdReferenceField(owner, referenceTable, column, row, rawValue, onValueChanged, onJumpToReference);

                if (column.IsReferenceCandidate)
                {
                    if (TableEditorVfxReferenceUtility.IsTabbedVfxReference(column))
                        return CreateTabbedVfxReferenceField(owner, column, rawValue, onValueChanged, onJumpToReference);

                    return CreateReferenceField(owner, referenceTable, column, row, rawValue, onValueChanged, onJumpToReference);
                }

                if (column.IsMultiReferenceCandidate)
                    return CreateMultiReferenceField(owner, referenceTable, column, row, rawValue, onValueChanged, onJumpToReference);
            }

            if (type == typeof(string) || type.IsArray)
            {
                TextField field = new TextField { value = rawValue, multiline = false, isDelayed = true };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, evt.newValue ?? string.Empty));
                return field;
            }

            if (type == typeof(int))
            {
                IntegerField field = new IntegerField { value = converted is int i ? i : 0, isDelayed = true };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(long))
            {
                LongField field = new LongField { value = converted is long l ? l : 0L, isDelayed = true };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(float))
            {
                FloatField field = new FloatField { value = converted is float f ? f : 0f, isDelayed = true };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(double))
            {
                DoubleField field = new DoubleField { value = converted is double d ? d : 0d, isDelayed = true };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(bool))
            {
                Toggle field = new Toggle { value = converted is bool b && b };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(Vector2))
            {
                Vector2Field field = new Vector2Field { value = converted is Vector2 v ? v : Vector2.zero };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(Vector3))
            {
                Vector3Field field = new Vector3Field { value = converted is Vector3 v ? v : Vector3.zero };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(Vector4))
            {
                Vector4Field field = new Vector4Field { value = converted is Vector4 v ? v : Vector4.zero };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(Color))
            {
                ColorField field = new ColorField { value = converted is Color c ? c : Color.white, showAlpha = true, hdr = false };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(Color32))
            {
                Color initial = converted is Color32 c ? (Color)c : Color.white;
                ColorField field = new ColorField { value = initial, showAlpha = true, hdr = false };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw((Color32)evt.newValue, type)));
                return field;
            }

            if (type.IsEnum)
            {
                EnumField field = new EnumField((Enum)converted);
                field.Init((Enum)converted);
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            TextField fallback = new TextField { value = rawValue, isDelayed = true };
            fallback.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, evt.newValue ?? string.Empty));
            return fallback;
        }


        private static VisualElement CreateTabbedVfxReferenceField(
            EditorWindow owner,
            TableEditorColumnDefinition column,
            string rawValue,
            Action<string, string> onValueChanged,
            Action<TableEditorTableDefinition, int> onJumpToReference)
        {
            int currentUid = ParseInt(rawValue);
            IReadOnlyList<SearchableDropdownUtility.OptionTab<TableEditorVfxReferenceOption>> tabs = TableEditorVfxReferenceUtility.BuildTabs();

            VisualElement root = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column }
            };

            IntegerField uidField = new IntegerField
            {
                value = currentUid,
                isDelayed = true,
            };
            root.Add(uidField);

            VisualElement buttonRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4f }
            };

            Button pickerButton = new Button
            {
                style = { flexGrow = 1f, marginRight = 4f }
            };
            buttonRow.Add(pickerButton);

            Button openButton = new Button(() =>
            {
                int uid = ParseInt(uidField.value.ToString(CultureInfo.InvariantCulture));
                TableEditorVfxReferenceUtility.JumpToReference(onJumpToReference, uid);
            })
            {
                text = "Open",
                style = { width = 60f }
            };
            buttonRow.Add(openButton);
            root.Add(buttonRow);

            void RefreshUi(int uid)
            {
                pickerButton.text = TableEditorVfxReferenceUtility.BuildButtonText(uid);
                openButton.SetEnabled(TableEditorVfxReferenceUtility.Contains(uid));
            }

            uidField.RegisterValueChangedCallback(evt =>
            {
                onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, typeof(int)));
                RefreshUi(evt.newValue);
            });

            if (owner != null)
            {
                SearchableDropdownUtility.BindUiToolkitButton(
                    owner,
                    pickerButton,
                    tabs,
                    () => TableEditorVfxReferenceUtility.GetSelectedTabId(uidField.value),
                    tabId => TableEditorVfxReferenceUtility.GetSelectedIndex(tabs, tabId, uidField.value),
                    (selectedTab, selectedOptionIndex, option) =>
                    {
                        int selectedUid = option.Data?.Item?.Uid ?? 0;
                        uidField.SetValueWithoutNotify(selectedUid);
                        onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(selectedUid, typeof(int)));
                        RefreshUi(selectedUid);
                    });
            }
            else
            {
                pickerButton.SetEnabled(false);
            }

            RefreshUi(currentUid);
            return root;
        }

        private static VisualElement CreateReferenceField(
            EditorWindow owner,
            TableEditorTableDefinition referenceTable,
            TableEditorColumnDefinition column,
            TableEditorDocumentRow row,
            string rawValue,
            Action<string, string> onValueChanged,
            Action<TableEditorTableDefinition, int> onJumpToReference)
        {
            VisualElement root = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            IntegerField uidField = new IntegerField { value = ParseInt(rawValue), isDelayed = true };
            root.Add(uidField);

            IReadOnlyList<TableEditorReferenceItem> items = TableEditorReferenceCache.GetItems(referenceTable);
            List<SearchableDropdownUtility.Option<TableEditorReferenceItem>> options = BuildUidOptions(items);

            VisualElement rowElement = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4f } };
            Button pickerButton = new Button { style = { flexGrow = 1f, marginRight = 4f } };
            rowElement.Add(pickerButton);

            Button openButton = new Button(() =>
            {
                int uid = ParseInt(row.Values.TryGetValue(column.HeaderName, out string latestRaw) ? latestRaw : rawValue);
                if (uid > 0)
                    onJumpToReference?.Invoke(referenceTable, uid);
            })
            {
                text = "Open",
                style = { width = 60f }
            };
            rowElement.Add(openButton);
            root.Add(rowElement);

            void RefreshReferenceUi(int uid)
            {
                openButton.SetEnabled(referenceTable != null && uid > 0);
                TableEditorReferenceItem currentItem = TableEditorReferenceCache.FindItem(referenceTable, uid);
                pickerButton.text = currentItem != null ? BuildItemText(currentItem, false) : $"Select {referenceTable?.TableKey ?? "Reference"}";
            }

            if (options.Count > 0 && owner != null)
            {
                SearchableDropdownUtility.BindUiToolkitButton(
                    owner,
                    pickerButton,
                    options,
                    () => FindSelectedUidIndex(items, ParseInt(row.Values.TryGetValue(column.HeaderName, out string latestRaw) ? latestRaw : rawValue)),
                    (selectedOptionIndex, option) =>
                    {
                        pickerButton.text = option.ToString();
                        uidField.SetValueWithoutNotify(option.Data.Uid);
                        onValueChanged?.Invoke(column.HeaderName, option.Data.Uid.ToString(CultureInfo.InvariantCulture));
                    });
            }
            else
            {
                pickerButton.SetEnabled(false);
            }

            uidField.RegisterValueChangedCallback(evt =>
            {
                onValueChanged?.Invoke(column.HeaderName, evt.newValue.ToString(CultureInfo.InvariantCulture));
                RefreshReferenceUi(evt.newValue);
            });
            RefreshReferenceUi(ParseInt(rawValue));

            if (referenceTable == null)
                root.Add(new HelpBox("참조 테이블을 찾지 못했습니다.", HelpBoxMessageType.Warning));

            return root;
        }

        private static VisualElement CreateStringIdReferenceField(
            EditorWindow owner,
            TableEditorTableDefinition referenceTable,
            TableEditorColumnDefinition column,
            TableEditorDocumentRow row,
            string rawValue,
            Action<string, string> onValueChanged,
            Action<TableEditorTableDefinition, int> onJumpToReference)
        {
            VisualElement root = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            TextField idField = new TextField { value = rawValue ?? string.Empty, multiline = false, isDelayed = true };
            root.Add(idField);

            IReadOnlyList<TableEditorReferenceItem> items = TableEditorReferenceCache.GetItems(referenceTable);
            List<SearchableDropdownUtility.Option<TableEditorReferenceItem>> options = BuildStringIdOptions(items);

            VisualElement rowElement = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4f } };
            Button pickerButton = new Button { style = { flexGrow = 1f, marginRight = 4f } };
            rowElement.Add(pickerButton);

            Button openButton = new Button(() =>
            {
                string id = row.Values.TryGetValue(column.HeaderName, out string latestRaw) ? latestRaw : rawValue;
                TableEditorReferenceItem item = TableEditorReferenceCache.FindItem(referenceTable, id);
                if (item != null && item.Uid > 0)
                    onJumpToReference?.Invoke(referenceTable, item.Uid);
            })
            {
                text = "Open",
                style = { width = 60f }
            };
            rowElement.Add(openButton);
            root.Add(rowElement);

            void RefreshReferenceUi(string stringId)
            {
                TableEditorReferenceItem currentItem = TableEditorReferenceCache.FindItem(referenceTable, stringId);
                openButton.SetEnabled(referenceTable != null && currentItem != null && currentItem.Uid > 0);
                pickerButton.text = currentItem != null ? BuildItemText(currentItem, true) : $"Select {referenceTable?.TableKey ?? "Reference"}";
            }

            if (options.Count > 0 && owner != null)
            {
                SearchableDropdownUtility.BindUiToolkitButton(
                    owner,
                    pickerButton,
                    options,
                    () => FindSelectedStringIdIndex(items, row.Values.TryGetValue(column.HeaderName, out string latestRaw) ? latestRaw : rawValue),
                    (selectedOptionIndex, option) =>
                    {
                        string nextValue = option.Data.StringId ?? string.Empty;
                        idField.SetValueWithoutNotify(nextValue);
                        pickerButton.text = option.ToString();
                        onValueChanged?.Invoke(column.HeaderName, nextValue);
                    });
            }
            else
            {
                pickerButton.SetEnabled(false);
            }

            idField.RegisterValueChangedCallback(evt =>
            {
                onValueChanged?.Invoke(column.HeaderName, evt.newValue ?? string.Empty);
                RefreshReferenceUi(evt.newValue ?? string.Empty);
            });
            RefreshReferenceUi(rawValue);

            if (referenceTable == null)
                root.Add(new HelpBox("참조 테이블을 찾지 못했습니다.", HelpBoxMessageType.Warning));

            return root;
        }

        private static VisualElement CreateMultiReferenceField(
            EditorWindow owner,
            TableEditorTableDefinition referenceTable,
            TableEditorColumnDefinition column,
            TableEditorDocumentRow row,
            string rawValue,
            Action<string, string> onValueChanged,
            Action<TableEditorTableDefinition, int> onJumpToReference)
        {
            VisualElement root = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            List<int> selectedUids = ParseIntList(rawValue);
            IReadOnlyList<TableEditorReferenceItem> items = TableEditorReferenceCache.GetItems(referenceTable);
            List<SearchableDropdownUtility.Option<TableEditorReferenceItem>> options = BuildUidOptions(items);

            VisualElement listRoot = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = 4f,
                }
            };
            root.Add(listRoot);

            void Commit() => onValueChanged?.Invoke(column.HeaderName, string.Join(",", selectedUids));

            void Refresh()
            {
                listRoot.Clear();

                if (selectedUids.Count == 0)
                {
                    listRoot.Add(new HelpBox($"{referenceTable?.TableKey ?? "Reference"} 항목이 없습니다.", HelpBoxMessageType.Info));
                    return;
                }

                for (int index = 0; index < selectedUids.Count; index++)
                {
                    int capturedIndex = index;
                    int currentUid = selectedUids[capturedIndex];

                    VisualElement itemRoot = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Column,
                            marginBottom = 4f,
                            paddingLeft = 4f,
                            paddingRight = 4f,
                            paddingTop = 4f,
                            paddingBottom = 4f,
                            borderBottomWidth = 1f,
                            borderTopWidth = 1f,
                            borderLeftWidth = 1f,
                            borderRightWidth = 1f,
                            borderBottomColor = new Color(0.23f, 0.23f, 0.23f),
                            borderTopColor = new Color(0.23f, 0.23f, 0.23f),
                            borderLeftColor = new Color(0.23f, 0.23f, 0.23f),
                            borderRightColor = new Color(0.23f, 0.23f, 0.23f),
                        }
                    };

                    IntegerField uidField = new IntegerField($"Element {capturedIndex + 1}")
                    {
                        value = currentUid,
                        isDelayed = true,
                    };
                    itemRoot.Add(uidField);

                    VisualElement buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4f } };
                    Button pickerButton = new Button { style = { flexGrow = 1f, marginRight = 4f } };
                    buttonRow.Add(pickerButton);

                    Button openButton = new Button(() =>
                    {
                        int uid = selectedUids.Count > capturedIndex ? selectedUids[capturedIndex] : 0;
                        if (uid > 0)
                            onJumpToReference?.Invoke(referenceTable, uid);
                    })
                    {
                        text = "Open",
                        style = { width = 60f, marginRight = 4f }
                    };
                    buttonRow.Add(openButton);

                    Button removeButton = new Button(() =>
                    {
                        if (capturedIndex < 0 || capturedIndex >= selectedUids.Count)
                            return;

                        selectedUids.RemoveAt(capturedIndex);
                        Commit();
                        Refresh();
                    })
                    {
                        text = "Remove",
                        style = { width = 80f }
                    };
                    buttonRow.Add(removeButton);
                    itemRoot.Add(buttonRow);

                    void RefreshItemUi(int uid)
                    {
                        openButton.SetEnabled(referenceTable != null && uid > 0);
                        TableEditorReferenceItem currentItem = TableEditorReferenceCache.FindItem(referenceTable, uid);
                        pickerButton.text = currentItem != null ? BuildItemText(currentItem, false) : $"Select {referenceTable?.TableKey ?? "Reference"}";
                    }

                    uidField.RegisterValueChangedCallback(evt =>
                    {
                        if (capturedIndex < 0 || capturedIndex >= selectedUids.Count)
                            return;

                        selectedUids[capturedIndex] = evt.newValue;
                        Commit();
                        RefreshItemUi(evt.newValue);
                    });

                    if (options.Count > 0 && owner != null)
                    {
                        SearchableDropdownUtility.BindUiToolkitButton(
                            owner,
                            pickerButton,
                            options,
                            () => FindSelectedUidIndex(items, selectedUids.Count > capturedIndex ? selectedUids[capturedIndex] : 0),
                            (selectedOptionIndex, option) =>
                            {
                                if (capturedIndex < 0 || capturedIndex >= selectedUids.Count)
                                    return;

                                selectedUids[capturedIndex] = option.Data.Uid;
                                uidField.SetValueWithoutNotify(option.Data.Uid);
                                Commit();
                                RefreshItemUi(option.Data.Uid);
                            });
                    }
                    else
                    {
                        pickerButton.SetEnabled(false);
                    }

                    RefreshItemUi(currentUid);
                    listRoot.Add(itemRoot);
                }
            }

            Button addButton = new Button(() =>
            {
                selectedUids.Add(0);
                Commit();
                Refresh();
            })
            {
                text = $"Add {referenceTable?.DisplayName ?? referenceTable?.TableKey ?? "Reference"}"
            };
            root.Add(addButton);

            if (referenceTable == null)
                root.Add(new HelpBox("참조 테이블을 찾지 못했습니다.", HelpBoxMessageType.Warning));

            Refresh();
            return root;
        }

        private static List<SearchableDropdownUtility.Option<TableEditorReferenceItem>> BuildUidOptions(IReadOnlyList<TableEditorReferenceItem> items)
        {
            List<SearchableDropdownUtility.Option<TableEditorReferenceItem>> options = new List<SearchableDropdownUtility.Option<TableEditorReferenceItem>>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                TableEditorReferenceItem item = items[i];
                options.Add(new SearchableDropdownUtility.Option<TableEditorReferenceItem>(item.Uid.ToString(CultureInfo.InvariantCulture), item.DisplayName, item));
            }
            return options;
        }

        private static List<SearchableDropdownUtility.Option<TableEditorReferenceItem>> BuildStringIdOptions(IReadOnlyList<TableEditorReferenceItem> items)
        {
            List<SearchableDropdownUtility.Option<TableEditorReferenceItem>> options = new List<SearchableDropdownUtility.Option<TableEditorReferenceItem>>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                TableEditorReferenceItem item = items[i];
                string key = string.IsNullOrWhiteSpace(item.StringId) ? item.Uid.ToString(CultureInfo.InvariantCulture) : item.StringId;
                options.Add(new SearchableDropdownUtility.Option<TableEditorReferenceItem>(key, item.DisplayName, item));
            }
            return options;
        }

        private static int FindSelectedUidIndex(IReadOnlyList<TableEditorReferenceItem> items, int currentUid)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Uid == currentUid)
                    return i;
            }
            return -1;
        }

        private static int FindSelectedStringIdIndex(IReadOnlyList<TableEditorReferenceItem> items, string currentId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].StringId, currentId, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static string BuildItemText(TableEditorReferenceItem item, bool preferStringId)
        {
            if (item == null)
                return string.Empty;

            string key = preferStringId && !string.IsNullOrWhiteSpace(item.StringId)
                ? item.StringId
                : item.Uid.ToString(CultureInfo.InvariantCulture);
            return $"{key}  |  {item.DisplayName}";
        }

        private static object GetDefaultValue(Type type)
        {
            if (type == null)
                return string.Empty;

            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static int ParseInt(string raw)
        {
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
        }

        private static List<int> ParseIntList(string raw)
        {
            List<int> values = new List<int>();
            if (string.IsNullOrWhiteSpace(raw))
                return values;

            string[] tokens = raw.Split(',');
            for (int i = 0; i < tokens.Length; i++)
            {
                if (int.TryParse(tokens[i]?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    values.Add(value);
            }

            return values;
        }
    }
}
