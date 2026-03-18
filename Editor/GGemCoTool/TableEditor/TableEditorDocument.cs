using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal enum TableEditorLineKind
    {
        Empty,
        Comment,
        Data,
    }

    internal sealed class TableEditorDocumentRow
    {
        public int StableId;
        public Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string CachedDisplayName;
    }

    internal sealed class TableEditorDocumentLine
    {
        public TableEditorLineKind Kind;
        public string RawText;
        public TableEditorDocumentRow Row;
    }

    internal sealed class TableEditorDocument
    {
        private static int _rowIdSeed = 1;

        public string AssetPath;
        public string NewLine = "\n";
        public List<string> Headers = new List<string>();
        public List<TableEditorDocumentLine> Lines = new List<TableEditorDocumentLine>();
        public bool IsDirty;

        public IEnumerable<TableEditorDocumentRow> GetRows()
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].Kind == TableEditorLineKind.Data && Lines[i].Row != null)
                    yield return Lines[i].Row;
            }
        }

        public static TableEditorDocument Load(string assetPath)
        {
            string fullPath = GetFullPath(assetPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"테이블 파일을 찾을 수 없습니다: {assetPath}", fullPath);

            string content = File.ReadAllText(fullPath, Encoding.UTF8);
            TableEditorDocument document = new TableEditorDocument
            {
                AssetPath = assetPath,
                NewLine = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n"
            };

            string[] normalizedLines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            if (normalizedLines.Length == 0)
                return document;

            string headerLine = normalizedLines[0].TrimEnd('\r');
            if (!string.IsNullOrWhiteSpace(headerLine))
                document.Headers.AddRange(headerLine.Split('\t'));

            for (int i = 1; i < normalizedLines.Length; i++)
            {
                string rawLine = normalizedLines[i].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    document.Lines.Add(new TableEditorDocumentLine { Kind = TableEditorLineKind.Empty, RawText = rawLine });
                    continue;
                }

                if (rawLine.StartsWith("#", StringComparison.Ordinal))
                {
                    document.Lines.Add(new TableEditorDocumentLine { Kind = TableEditorLineKind.Comment, RawText = rawLine });
                    continue;
                }

                string[] values = rawLine.Split('\t');
                if (values.Length < document.Headers.Count)
                    Array.Resize(ref values, document.Headers.Count);

                TableEditorDocumentRow row = new TableEditorDocumentRow
                {
                    StableId = _rowIdSeed++
                };

                for (int c = 0; c < document.Headers.Count; c++)
                {
                    string header = document.Headers[c];
                    row.Values[header] = c < values.Length ? values[c] ?? string.Empty : string.Empty;
                }

                document.Lines.Add(new TableEditorDocumentLine
                {
                    Kind = TableEditorLineKind.Data,
                    Row = row,
                    RawText = rawLine,
                });
            }

            return document;
        }

        public void MergeHeaders(IReadOnlyList<TableEditorColumnDefinition> columns)
        {
            if (columns == null)
                return;

            for (int i = 0; i < columns.Count; i++)
            {
                string header = columns[i].HeaderName;
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                if (!Headers.Contains(header))
                    Headers.Add(header);
            }

            foreach (TableEditorDocumentRow row in GetRows())
            {
                for (int i = 0; i < Headers.Count; i++)
                {
                    if (!row.Values.ContainsKey(Headers[i]))
                        row.Values[Headers[i]] = string.Empty;
                }
            }
        }

        public TableEditorDocumentRow AddRow()
        {
            TableEditorDocumentRow row = new TableEditorDocumentRow
            {
                StableId = _rowIdSeed++
            };

            for (int i = 0; i < Headers.Count; i++)
                row.Values[Headers[i]] = string.Empty;

            Lines.Add(new TableEditorDocumentLine
            {
                Kind = TableEditorLineKind.Data,
                Row = row,
                RawText = string.Empty,
            });

            IsDirty = true;
            return row;
        }

        public TableEditorDocumentRow DuplicateRow(TableEditorDocumentRow source)
        {
            if (source == null)
                return null;

            TableEditorDocumentRow row = new TableEditorDocumentRow
            {
                StableId = _rowIdSeed++
            };

            foreach (var pair in source.Values)
                row.Values[pair.Key] = pair.Value;

            Lines.Add(new TableEditorDocumentLine
            {
                Kind = TableEditorLineKind.Data,
                Row = row,
                RawText = string.Empty,
            });

            IsDirty = true;
            return row;
        }

        public void RemoveRow(TableEditorDocumentRow target)
        {
            if (target == null)
                return;

            Lines.RemoveAll(line => line.Kind == TableEditorLineKind.Data && line.Row == target);
            IsDirty = true;
        }

        public void Save()
        {
            StringBuilder builder = new StringBuilder(4096);
            builder.Append(string.Join("\t", Headers));
            builder.Append(NewLine);

            for (int i = 0; i < Lines.Count; i++)
            {
                TableEditorDocumentLine line = Lines[i];
                switch (line.Kind)
                {
                    case TableEditorLineKind.Empty:
                        builder.Append(NewLine);
                        break;
                    case TableEditorLineKind.Comment:
                        builder.Append(line.RawText ?? string.Empty);
                        builder.Append(NewLine);
                        break;
                    case TableEditorLineKind.Data:
                        builder.Append(SerializeRow(line.Row));
                        builder.Append(NewLine);
                        break;
                }
            }

            string fullPath = GetFullPath(AssetPath);
            File.WriteAllText(fullPath, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();
            IsDirty = false;
        }

        private string SerializeRow(TableEditorDocumentRow row)
        {
            if (row == null)
                return string.Empty;

            string[] values = new string[Headers.Count];
            for (int i = 0; i < Headers.Count; i++)
            {
                row.Values.TryGetValue(Headers[i], out string value);
                values[i] = (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", "\\n");
            }

            return string.Join("\t", values);
        }

        private static string GetFullPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? string.Empty, assetPath);
        }
    }
}
