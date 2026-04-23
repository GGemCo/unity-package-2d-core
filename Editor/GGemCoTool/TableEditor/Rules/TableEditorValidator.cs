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
            ITableEditorTableRuleProvider ruleProvider = TableEditorRuleProviderRegistry.GetProvider(definition);
            foreach (TableEditorDocumentRow row in document.GetRows())
            {
                object rowObject = TableEditorValueUtility.BuildRowObject(definition, row, out List<string> fieldErrors);
                for (int i = 0; i < fieldErrors.Count; i++)
                {
                    messages.Add(new TableEditorValidationMessage
                    {
                        Severity = TableEditorValidationSeverity.Error,
                        Message = fieldErrors[i],
                        RowStableId = row.stableId,
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
                            RowStableId = row.stableId,
                        });
                    }
                    else if (!uniqueUids.Add(uid))
                    {
                        messages.Add(new TableEditorValidationMessage
                        {
                            Severity = TableEditorValidationSeverity.Error,
                            Message = $"중복 Uid: {uid}",
                            RowStableId = row.stableId,
                        });
                    }
                }

                ValidateReferenceFields(row, columnMap, messages);
                ruleProvider?.ValidateRow(definition, row, columnMap, messages);
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
                if (!columnMap.TryGetValue(pair.Key, out TableEditorColumnDefinition column) || !column.HasReferenceCandidate)
                    continue;

                TableEditorReferenceRule rule = column.ResolveReferenceRule(row);
                TableEditorTableDefinition referenceTable = column.GetReferenceTable(rule);
                if (rule != null && rule.ValueKind == TableEditorReferenceValueKind.StringId)
                {
                    if (string.IsNullOrWhiteSpace(pair.Value))
                        continue;

                    AddReferenceWarningIfMissing(row, messages, referenceTable, pair.Key, pair.Value);
                    continue;
                }

                if (column.IsReferenceCandidate)
                {
                    if (!int.TryParse(pair.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int uid) || uid <= 0)
                        continue;

                    AddReferenceWarningIfMissing(row, messages, referenceTable, pair.Key, uid);;
                    continue;
                }

                if (!column.IsMultiReferenceCandidate || string.IsNullOrWhiteSpace(pair.Value))
                    continue;

                string[] tokens = pair.Value.Split(',');
                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i]?.Trim();
                    if (string.IsNullOrWhiteSpace(token))
                        continue;

                    if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int uid) || uid <= 0)
                        continue;

                    AddReferenceWarningIfMissing(row, messages, referenceTable, pair.Key, uid);
                }
            }
        }

        private static void AddReferenceWarningIfMissing(
            TableEditorDocumentRow row,
            List<TableEditorValidationMessage> messages,
            TableEditorTableDefinition referenceTable,
            string headerName,
            int uid)
        {
            if (TableEditorVfxReferenceUtility.IsTabbedVfxReference(headerName))
            {
                if (TableEditorVfxReferenceUtility.Contains(uid))
                    return;

                messages.Add(new TableEditorValidationMessage
                {
                    Severity = TableEditorValidationSeverity.Warning,
                    Message = $"참조 없음: {headerName} -> vfx:{uid}",
                    RowStableId = row.stableId,
                });
                return;
            }

            if (TableEditorReferenceCache.Contains(referenceTable, uid))
                return;

            messages.Add(new TableEditorValidationMessage
            {
                Severity = TableEditorValidationSeverity.Warning,
                Message = $"참조 없음: {headerName} -> {referenceTable?.TableKey}:{uid}",
                RowStableId = row.stableId,
            });
        }

        private static void AddReferenceWarningIfMissing(
            TableEditorDocumentRow row,
            List<TableEditorValidationMessage> messages,
            TableEditorTableDefinition referenceTable,
            string headerName,
            string stringId)
        {
            if (TableEditorReferenceCache.Contains(referenceTable, stringId))
                return;

            messages.Add(new TableEditorValidationMessage
            {
                Severity = TableEditorValidationSeverity.Warning,
                Message = $"참조 없음: {headerName} -> {referenceTable?.TableKey}:{stringId}",
                RowStableId = row.stableId,
            });
        }
    }
}
