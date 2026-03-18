using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal static class TableEditorGui
    {
        public static bool DrawCellEditor(TableEditorColumnDefinition column, TableEditorDocumentRow row, Action<TableEditorTableDefinition, int> onJumpToReference, TableEditorTableDefinition currentDefinition)
        {
            if (column == null || row == null)
                return false;

            row.Values.TryGetValue(column.HeaderName, out string rawValue);
            string nextRaw = rawValue ?? string.Empty;
            bool changed = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(column.HeaderName, EditorStyles.boldLabel);

            if (column.MemberInfo == null)
            {
                EditorGUI.BeginChangeCheck();
                nextRaw = EditorGUILayout.TextField(nextRaw);
                if (EditorGUI.EndChangeCheck())
                    changed = true;
            }
            else
            {
                Type type = column.ValueType;
                if (!TableEditorValueUtility.TryConvertFromRaw(nextRaw, type, out object converted, out _))
                    converted = GetDefaultValue(type);

                if (type == typeof(string))
                {
                    EditorGUI.BeginChangeCheck();
                    nextRaw = EditorGUILayout.TextField(nextRaw);
                    if (EditorGUI.EndChangeCheck())
                        changed = true;
                }
                else if (type == typeof(int))
                {
                    EditorGUI.BeginChangeCheck();
                    int value = EditorGUILayout.IntField((int)(converted ?? 0));
                    if (EditorGUI.EndChangeCheck())
                    {
                        nextRaw = TableEditorValueUtility.ConvertToRaw(value, type);
                        changed = true;
                    }
                }
                else if (type == typeof(long))
                {
                    EditorGUI.BeginChangeCheck();
                    long value = EditorGUILayout.LongField((long)(converted ?? 0L));
                    if (EditorGUI.EndChangeCheck())
                    {
                        nextRaw = TableEditorValueUtility.ConvertToRaw(value, type);
                        changed = true;
                    }
                }
                else if (type == typeof(float))
                {
                    EditorGUI.BeginChangeCheck();
                    float value = EditorGUILayout.FloatField((float)(converted ?? 0f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        nextRaw = TableEditorValueUtility.ConvertToRaw(value, type);
                        changed = true;
                    }
                }
                else if (type == typeof(bool))
                {
                    EditorGUI.BeginChangeCheck();
                    bool value = EditorGUILayout.Toggle((bool)(converted ?? false));
                    if (EditorGUI.EndChangeCheck())
                    {
                        nextRaw = TableEditorValueUtility.ConvertToRaw(value, type);
                        changed = true;
                    }
                }
                else if (type == typeof(Vector2))
                {
                    EditorGUI.BeginChangeCheck();
                    Vector2 value = EditorGUILayout.Vector2Field(GUIContent.none, (Vector2)(converted ?? Vector2.zero));
                    if (EditorGUI.EndChangeCheck())
                    {
                        nextRaw = TableEditorValueUtility.ConvertToRaw(value, type);
                        changed = true;
                    }
                }
                else if (type.IsEnum)
                {
                    EditorGUI.BeginChangeCheck();
                    Enum value = (Enum)converted;
                    Enum next = EditorGUILayout.EnumPopup(value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        nextRaw = TableEditorValueUtility.ConvertToRaw(next, type);
                        changed = true;
                    }
                }
                else if (type.IsArray)
                {
                    EditorGUI.BeginChangeCheck();
                    nextRaw = EditorGUILayout.TextField(nextRaw);
                    if (EditorGUI.EndChangeCheck())
                        changed = true;
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    nextRaw = EditorGUILayout.TextField(nextRaw);
                    if (EditorGUI.EndChangeCheck())
                        changed = true;
                }
            }

            if (DrawReferenceControls(column, row, onJumpToReference, currentDefinition))
                changed = true;

            if (!column.ExistsInRowType)
            {
                EditorGUILayout.HelpBox("현재 row 클래스에 없는 컬럼입니다. 저장은 유지되지만 타입 검증은 제한됩니다.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            if (changed)
                row.Values[column.HeaderName] = nextRaw;

            return changed;
        }

        public static void DrawValidationSummary(IReadOnlyList<TableEditorValidationMessage> messages, int selectedRowStableId)
        {
            if (messages == null || messages.Count == 0)
            {
                EditorGUILayout.HelpBox("검증 결과가 없습니다. Validate 버튼으로 다시 확인하세요.", MessageType.Info);
                return;
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

            EditorGUILayout.HelpBox($"Errors: {errorCount}, Warnings: {warningCount}", errorCount > 0 ? MessageType.Error : MessageType.Warning);

            for (int i = 0; i < messages.Count; i++)
            {
                TableEditorValidationMessage message = messages[i];
                if (selectedRowStableId > 0 && message.RowStableId > 0 && message.RowStableId != selectedRowStableId)
                    continue;

                MessageType messageType = message.Severity == TableEditorValidationSeverity.Error ? MessageType.Error : MessageType.Warning;
                EditorGUILayout.HelpBox(message.Message, messageType);
            }
        }

        private static object GetDefaultValue(Type type)
        {
            if (type == null)
                return string.Empty;

            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

        private static bool DrawReferenceControls(TableEditorColumnDefinition column, TableEditorDocumentRow row, Action<TableEditorTableDefinition, int> onJumpToReference, TableEditorTableDefinition currentDefinition)
        {
            TableEditorTableDefinition referenceTable = TableEditorRegistry.FindReferenceTable(column.HeaderName);
            if (referenceTable == null)
                return false;

            bool changed = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Ref: {referenceTable.TableKey}", GUILayout.Width(90f));

            if (int.TryParse(row.Values[column.HeaderName], out int uid) && uid > 0)
            {
                if (GUILayout.Button("Open Reference", GUILayout.Width(120f)))
                    onJumpToReference?.Invoke(referenceTable, uid);

                IReadOnlyList<TableEditorReferenceItem> items = TableEditorReferenceCache.GetItems(referenceTable);
                if (items.Count > 0)
                {
                    string[] names = new string[items.Count + 1];
                    names[0] = "Select...";
                    int selectedIndex = 0;
                    for (int i = 0; i < items.Count; i++)
                    {
                        names[i + 1] = items[i].DisplayName;
                        if (items[i].Uid == uid)
                            selectedIndex = i + 1;
                    }

                    EditorGUI.BeginChangeCheck();
                    int nextIndex = EditorGUILayout.Popup(selectedIndex, names);
                    if (EditorGUI.EndChangeCheck() && nextIndex > 0)
                    {
                        row.Values[column.HeaderName] = items[nextIndex - 1].Uid.ToString();
                        changed = true;
                    }
                }
            }
            else
            {
                GUILayout.Label("Uid 값 없음", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
            return changed;
        }
    }
}
