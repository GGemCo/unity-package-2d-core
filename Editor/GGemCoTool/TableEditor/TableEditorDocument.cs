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

    [Serializable]
    public sealed class TableEditorDocumentRow
    {
        public int stableId;
        public Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string cachedDisplayName;
    }

    [Serializable]
    internal sealed class TableEditorDocumentLine
    {
        public TableEditorLineKind Kind;
        public string RawText;
        public TableEditorDocumentRow Row;
    }

    [Serializable]
    internal sealed class TableEditorDocument
    {
        [Serializable]
        private sealed class SnapshotRoot
        {
            public string assetPath;
            public string newLine;
            public bool isDirty;
            public List<string> headers = new List<string>();
            public List<SnapshotLine> lines = new List<SnapshotLine>();
        }

        [Serializable]
        private sealed class SnapshotLine
        {
            public int kind;
            public string rawText;
            public SnapshotRow row;
        }

        [Serializable]
        private sealed class SnapshotRow
        {
            public int stableId;
            public string cachedDisplayName;
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();
        }

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
                    stableId = _rowIdSeed++
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
                stableId = _rowIdSeed++
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
                stableId = _rowIdSeed++
            };

            foreach (KeyValuePair<string, string> pair in source.Values)
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

        public bool SetCellValue(TableEditorDocumentRow row, string header, string value)
        {
            if (row == null || string.IsNullOrWhiteSpace(header))
                return false;

            row.Values.TryGetValue(header, out string current);
            current ??= string.Empty;
            value ??= string.Empty;
            if (string.Equals(current, value, StringComparison.Ordinal))
                return false;

            row.Values[header] = value;
            IsDirty = true;
            return true;
        }

        public void Save()
        {
            StringBuilder builder = new StringBuilder(4096);
            builder.Append(string.Join("	", Headers));

            for (int i = 0; i < Lines.Count; i++)
            {
                TableEditorDocumentLine line = Lines[i];
                switch (line.Kind)
                {
                    case TableEditorLineKind.Empty:
                        continue;
                    case TableEditorLineKind.Comment:
                        builder.Append(NewLine);
                        builder.Append(line.RawText ?? string.Empty);
                        break;
                    case TableEditorLineKind.Data:
                        builder.Append(NewLine);
                        builder.Append(SerializeRow(line.Row));
                        break;
                }
            }

            builder.Append(NewLine);

            string fullPath = GetFullPath(AssetPath);
            File.WriteAllText(fullPath, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();
            IsDirty = false;
        }

        public string ToSnapshotJson()
        {
            SnapshotRoot snapshot = new SnapshotRoot
            {
                assetPath = AssetPath,
                newLine = NewLine,
                isDirty = IsDirty,
                headers = new List<string>(Headers),
            };

            for (int i = 0; i < Lines.Count; i++)
            {
                TableEditorDocumentLine line = Lines[i];
                SnapshotLine lineSnapshot = new SnapshotLine
                {
                    kind = (int)line.Kind,
                    rawText = line.RawText,
                };

                if (line.Row != null)
                {
                    SnapshotRow rowSnapshot = new SnapshotRow
                    {
                        stableId = line.Row.stableId,
                        cachedDisplayName = line.Row.cachedDisplayName,
                    };

                    foreach (KeyValuePair<string, string> pair in line.Row.Values)
                    {
                        rowSnapshot.keys.Add(pair.Key);
                        rowSnapshot.values.Add(pair.Value ?? string.Empty);
                    }

                    lineSnapshot.row = rowSnapshot;
                }

                snapshot.lines.Add(lineSnapshot);
            }

            return JsonUtility.ToJson(snapshot, false);
        }

        public static TableEditorDocument FromSnapshotJson(string snapshotJson)
        {
            if (string.IsNullOrWhiteSpace(snapshotJson))
                return null;

            SnapshotRoot snapshot = JsonUtility.FromJson<SnapshotRoot>(snapshotJson);
            if (snapshot == null)
                return null;

            TableEditorDocument document = new TableEditorDocument
            {
                AssetPath = snapshot.assetPath,
                NewLine = string.IsNullOrEmpty(snapshot.newLine) ? "\n" : snapshot.newLine,
                IsDirty = snapshot.isDirty,
                Headers = snapshot.headers ?? new List<string>(),
            };

            if (snapshot.lines != null)
            {
                for (int i = 0; i < snapshot.lines.Count; i++)
                {
                    SnapshotLine lineSnapshot = snapshot.lines[i];
                    TableEditorDocumentLine line = new TableEditorDocumentLine
                    {
                        Kind = (TableEditorLineKind)lineSnapshot.kind,
                        RawText = lineSnapshot.rawText,
                    };

                    if (lineSnapshot.row != null)
                    {
                        TableEditorDocumentRow row = new TableEditorDocumentRow
                        {
                            stableId = lineSnapshot.row.stableId,
                            cachedDisplayName = lineSnapshot.row.cachedDisplayName,
                        };

                        int valueCount = Math.Min(lineSnapshot.row.keys.Count, lineSnapshot.row.values.Count);
                        for (int k = 0; k < valueCount; k++)
                            row.Values[lineSnapshot.row.keys[k]] = lineSnapshot.row.values[k] ?? string.Empty;

                        line.Row = row;
                        if (row.stableId >= _rowIdSeed)
                            _rowIdSeed = row.stableId + 1;
                    }

                    document.Lines.Add(line);
                }
            }

            return document;
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
