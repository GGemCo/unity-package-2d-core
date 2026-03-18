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
            row.CachedDisplayName = TableEditorReflectionUtility.GetDisplayName(previewObject, 0);

            Label title = new Label(row.CachedDisplayName)
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

            for (int i = 0; i < columns.Count; i++)
            {
                TableEditorColumnDefinition column = columns[i];
                root.Add(CreateField(owner, currentDefinition, column, row, onValueChanged, onJumpToReference));
            }

            return root;
        }

        public static VisualElement BuildValidationView(IReadOnlyList<TableEditorValidationMessage> messages, int selectedRowStableId, bool showOnlySelected)
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

            int errorCount = 0;
            int warningCount = 0;
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Severity == TableEditorValidationSeverity.Error)
                    errorCount++;
                else if (messages[i].Severity == TableEditorValidationSeverity.Warning)
                    warningCount++;
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

        private static VisualElement CreateField(
            EditorWindow owner,
            TableEditorTableDefinition currentDefinition,
            TableEditorColumnDefinition column,
            TableEditorDocumentRow row,
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

            Label label = new Label(column.HeaderName)
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4f }
            };
            container.Add(label);

            row.Values.TryGetValue(column.HeaderName, out string rawValue);

            VisualElement input = CreateInputField(owner, currentDefinition, column, row, rawValue ?? string.Empty, onValueChanged, onJumpToReference);
            if (input != null)
                container.Add(input);

            if (!column.ExistsInRowType)
                container.Add(new HelpBox("현재 row 클래스에 없는 컬럼입니다. 저장은 유지되지만 타입 검증은 제한됩니다.", HelpBoxMessageType.Info));

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

            if (column.IsReferenceCandidate)
                return CreateReferenceField(owner, currentDefinition, column, row, rawValue, onValueChanged, onJumpToReference);

            if (column.IsMultiReferenceCandidate)
                return CreateMultiReferenceField(owner, currentDefinition, column, row, rawValue, onValueChanged, onJumpToReference);

            if (type == typeof(string) || type.IsArray)
            {
                TextField field = new TextField { value = rawValue, multiline = false };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, evt.newValue ?? string.Empty));
                return field;
            }

            if (type == typeof(int))
            {
                IntegerField field = new IntegerField { value = converted is int i ? i : 0 };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(long))
            {
                LongField field = new LongField { value = converted is long l ? l : 0L };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(float))
            {
                FloatField field = new FloatField { value = converted is float f ? f : 0f };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, TableEditorValueUtility.ConvertToRaw(evt.newValue, type)));
                return field;
            }

            if (type == typeof(double))
            {
                DoubleField field = new DoubleField { value = converted is double d ? d : 0d };
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

            TextField fallback = new TextField { value = rawValue };
            fallback.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(column.HeaderName, evt.newValue ?? string.Empty));
            return fallback;
        }

        private static VisualElement CreateReferenceField(
            EditorWindow owner,
            TableEditorTableDefinition currentDefinition,
            TableEditorColumnDefinition column,
            TableEditorDocumentRow row,
            string rawValue,
            Action<string, string> onValueChanged,
            Action<TableEditorTableDefinition, int> onJumpToReference)
        {
            VisualElement root = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            IntegerField uidField = new IntegerField { value = ParseInt(rawValue) };
            root.Add(uidField);

            TableEditorTableDefinition referenceTable = column.ReferenceTable;
            IReadOnlyList<TableEditorReferenceItem> items = TableEditorReferenceCache.GetItems(referenceTable);
            List<SearchableDropdownUtility.Option<TableEditorReferenceItem>> options = new List<SearchableDropdownUtility.Option<TableEditorReferenceItem>>(items.Count);
            int selectedIndex = -1;
            int currentUid = ParseInt(rawValue);

            for (int i = 0; i < items.Count; i++)
            {
                TableEditorReferenceItem item = items[i];
                options.Add(new SearchableDropdownUtility.Option<TableEditorReferenceItem>(item.Uid.ToString(CultureInfo.InvariantCulture), item.DisplayName, item));
                if (item.Uid == currentUid)
                    selectedIndex = i;
            }

            VisualElement rowElement = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4f } };
            Button pickerButton = new Button
            {
                text = selectedIndex >= 0 ? options[selectedIndex].ToString() : $"Select {referenceTable?.TableKey ?? "Reference"}",
                style = { flexGrow = 1f, marginRight = 4f }
            };

            if (options.Count > 0 && owner != null)
            {
                SearchableDropdownUtility.BindUiToolkitButton(
                    owner,
                    pickerButton,
                    options,
                    () =>
                    {
                        int latestUid = ParseInt(row.Values.TryGetValue(column.HeaderName, out string latestRaw) ? latestRaw : rawValue);
                        for (int i = 0; i < items.Count; i++)
                        {
                            if (items[i].Uid == latestUid)
                                return i;
                        }

                        return -1;
                    },
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
                pickerButton.text = currentItem != null
                    ? $"{uid}  |  {currentItem.DisplayName}"
                    : $"Select {referenceTable?.TableKey ?? "Reference"}";
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

        private static VisualElement CreateMultiReferenceField(
            EditorWindow owner,
            TableEditorTableDefinition currentDefinition,
            TableEditorColumnDefinition column,
            TableEditorDocumentRow row,
            string rawValue,
            Action<string, string> onValueChanged,
            Action<TableEditorTableDefinition, int> onJumpToReference)
        {
            VisualElement root = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            TableEditorTableDefinition referenceTable = column.ReferenceTable;

            List<int> selectedUids = ParseIntList(rawValue);
            IReadOnlyList<TableEditorReferenceItem> items = TableEditorReferenceCache.GetItems(referenceTable);
            List<SearchableDropdownUtility.Option<TableEditorReferenceItem>> options = new List<SearchableDropdownUtility.Option<TableEditorReferenceItem>>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                TableEditorReferenceItem item = items[i];
                options.Add(new SearchableDropdownUtility.Option<TableEditorReferenceItem>(item.Uid.ToString(CultureInfo.InvariantCulture), item.DisplayName, item));
            }

            VisualElement listRoot = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = 4f,
                }
            };
            root.Add(listRoot);

            void Commit()
            {
                onValueChanged?.Invoke(column.HeaderName, string.Join(",", selectedUids));
            }

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
                    };
                    itemRoot.Add(uidField);

                    VisualElement buttonRow = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center,
                            marginTop = 4f,
                        }
                    };

                    Button pickerButton = new Button
                    {
                        style = { flexGrow = 1f, marginRight = 4f },
                    };
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
                        pickerButton.text = currentItem != null
                            ? $"{uid}  |  {currentItem.DisplayName}"
                            : $"Select {referenceTable?.TableKey ?? "Reference"}";
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
                            () =>
                            {
                                int latestUid = selectedUids.Count > capturedIndex ? selectedUids[capturedIndex] : 0;
                                for (int optionIndex = 0; optionIndex < items.Count; optionIndex++)
                                {
                                    if (items[optionIndex].Uid == latestUid)
                                        return optionIndex;
                                }

                                return -1;
                            },
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
