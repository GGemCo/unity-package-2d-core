using System;
using System.Collections.Generic;
using System.Globalization;

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

            Dictionary<string, TableEditorColumnDefinition> columnMap = new Dictionary<string, TableEditorColumnDefinition>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<TableEditorColumnDefinition> columns = definition.BuildColumns(document.Headers);
            for (int i = 0; i < columns.Count; i++)
                columnMap[columns[i].HeaderName] = columns[i];

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
                    if (!int.TryParse(uidRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int uid))
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

                ValidateReferenceFields(row, columnMap, messages);
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

        private static void ValidateReferenceFields(TableEditorDocumentRow row, IReadOnlyDictionary<string, TableEditorColumnDefinition> columnMap, List<TableEditorValidationMessage> messages)
        {
            foreach (KeyValuePair<string, string> pair in row.Values)
            {
                if (!columnMap.TryGetValue(pair.Key, out TableEditorColumnDefinition column) || !column.IsReferenceCandidate)
                    continue;

                if (!int.TryParse(pair.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int uid) || uid <= 0)
                    continue;

                if (!TableEditorReferenceCache.Contains(column.ReferenceTable, uid))
                {
                    messages.Add(new TableEditorValidationMessage
                    {
                        Severity = TableEditorValidationSeverity.Warning,
                        Message = $"참조 없음: {pair.Key} -> {column.ReferenceTable.TableKey}:{uid}",
                        RowStableId = row.StableId,
                    });
                }
            }
        }
    }
}
