using System;
using System.Collections.Generic;
using System.Reflection;

namespace GGemCo2DCoreEditor
{
    internal enum TableEditorValidationSeverity
    {
        Info,
        Warning,
        Error,
    }

    internal sealed class TableEditorValidationMessage
    {
        public TableEditorValidationSeverity Severity;
        public string Message;
        public int RowStableId;
    }

    internal static class TableEditorValidator
    {
        public static List<TableEditorValidationMessage> Validate(TableEditorTableDefinition definition, TableEditorDocument document)
        {
            List<TableEditorValidationMessage> messages = new List<TableEditorValidationMessage>();
            if (definition == null || document == null)
                return messages;

            HashSet<int> uniqueUids = new HashSet<int>();
            foreach (TableEditorDocumentRow row in document.GetRows())
            {
                object rowObject = TableEditorValueUtility.BuildRowObject(definition, row, out List<string> fieldErrors);
                for (int i = 0; i < fieldErrors.Count; i++)
                {
                    messages.Add(new TableEditorValidationMessage
                    {
                        Severity = TableEditorValidationSeverity.Error,
                        Message = fieldErrors[i],
                        RowStableId = row.StableId,
                    });
                }

                string uidRaw = TableEditorValueUtility.GetRowUidRaw(row);
                if (!string.IsNullOrWhiteSpace(uidRaw))
                {
                    if (!int.TryParse(uidRaw, out int uid))
                    {
                        messages.Add(new TableEditorValidationMessage
                        {
                            Severity = TableEditorValidationSeverity.Error,
                            Message = "Uid 파싱 실패",
                            RowStableId = row.StableId,
                        });
                    }
                    else if (!uniqueUids.Add(uid))
                    {
                        messages.Add(new TableEditorValidationMessage
                        {
                            Severity = TableEditorValidationSeverity.Error,
                            Message = $"중복 Uid: {uid}",
                            RowStableId = row.StableId,
                        });
                    }
                }

                if (rowObject != null)
                {
                    ValidateReferenceFields(row, rowObject, messages);
                }
            }

            try
            {
                string content = TableEditorValueUtility.BuildTempContent(document);
                object instance = definition.CreateTableInstanceAndLoad(content);
                if (instance == null)
                {
                    messages.Add(new TableEditorValidationMessage
                    {
                        Severity = TableEditorValidationSeverity.Warning,
                        Message = "런타임 파서 검증 인스턴스를 만들지 못했습니다.",
                        RowStableId = -1,
                    });
                }
            }
            catch (Exception ex)
            {
                messages.Add(new TableEditorValidationMessage
                {
                    Severity = TableEditorValidationSeverity.Error,
                    Message = $"런타임 파서 검증 실패: {ex.Message}",
                    RowStableId = -1,
                });
            }

            return messages;
        }

        private static void ValidateReferenceFields(TableEditorDocumentRow row, object rowObject, List<TableEditorValidationMessage> messages)
        {
            Type rowType = rowObject.GetType();
            foreach (MemberInfo member in TableEditorReflectionUtility.GetEditableMembers(rowType))
            {
                Type memberType = TableEditorReflectionUtility.GetMemberType(member);
                TableEditorTableDefinition referenceTable = TableEditorRegistry.FindReferenceTable(member.Name);
                if (referenceTable == null)
                    continue;

                object value = TableEditorReflectionUtility.GetValue(rowObject, member);
                if (memberType == typeof(int))
                {
                    int uid = (int)(value ?? 0);
                    if (uid <= 0)
                        continue;

                    if (!TableEditorReferenceCache.Contains(referenceTable, uid))
                    {
                        messages.Add(new TableEditorValidationMessage
                        {
                            Severity = TableEditorValidationSeverity.Warning,
                            Message = $"참조 없음: {member.Name} -> {referenceTable.TableKey}:{uid}",
                            RowStableId = row.StableId,
                        });
                    }
                }
            }
        }
    }
}
