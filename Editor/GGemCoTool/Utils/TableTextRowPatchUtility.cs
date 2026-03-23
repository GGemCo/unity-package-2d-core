using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 탭(\t) 구분 텍스트 테이블 파일에서 특정 키 행만 교체/추가하는 공용 유틸리티입니다.
    /// - 실제 파일의 헤더 순서를 기준으로 직렬화합니다.
    /// - 주석/다른 행/정렬은 최대한 그대로 유지합니다.
    /// - 대상 키 행이 없으면 파일 끝에 append 합니다.
    /// </summary>
    public static class TableTextRowPatchUtility
    {
        public static bool TryPatchRowByUid<TRow>(
            string assetPath,
            int uid,
            TRow row,
            Func<TRow, IReadOnlyList<string>, string> serializeRow,
            out string error)
        {
            return TryPatchRowByIntKey(assetPath, "Uid", uid, row, serializeRow, out error);
        }

        public static bool TryPatchRowByIntKey<TRow>(
            string assetPath,
            string keyHeaderName,
            int keyValue,
            TRow row,
            Func<TRow, IReadOnlyList<string>, string> serializeRow,
            out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                error = "assetPath 가 비어있습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(keyHeaderName))
            {
                error = "keyHeaderName 이 비어있습니다.";
                return false;
            }

            if (keyValue <= 0)
            {
                error = $"잘못된 키 값입니다. {keyHeaderName}={keyValue}";
                return false;
            }

            if (row == null)
            {
                error = "저장할 Row 데이터가 없습니다.";
                return false;
            }

            if (serializeRow == null)
            {
                error = "serializeRow 가 null 입니다.";
                return false;
            }

            try
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string fullPath = Path.Combine(projectRoot ?? string.Empty, assetPath);
                if (!File.Exists(fullPath))
                {
                    error = $"테이블 파일을 찾을 수 없습니다: {assetPath}";
                    return false;
                }

                string content = File.ReadAllText(fullPath, Encoding.UTF8);
                string newline = DetectNewline(content);
                List<string> lines = SplitLines(content);
                if (lines.Count == 0)
                {
                    error = $"테이블 파일이 비어있습니다: {assetPath}";
                    return false;
                }

                string headerLine = lines[0].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    error = $"헤더가 비어있습니다: {assetPath}";
                    return false;
                }

                string[] headers = headerLine.Split('\t');
                int keyColumnIndex = Array.FindIndex(headers, h => string.Equals(h?.Trim(), keyHeaderName, StringComparison.Ordinal));
                if (keyColumnIndex < 0)
                {
                    error = $"{keyHeaderName} 헤더를 찾을 수 없습니다: {assetPath}";
                    return false;
                }

                string serializedRow = serializeRow(row, headers)?.TrimEnd('\r', '\n') ?? string.Empty;
                if (string.IsNullOrWhiteSpace(serializedRow))
                {
                    error = $"직렬화된 Row가 비어있습니다. {keyHeaderName}={keyValue}";
                    return false;
                }

                bool replaced = false;
                for (int i = 1; i < lines.Count; i++)
                {
                    string line = lines[i];
                    string trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
                        continue;

                    string[] values = line.TrimEnd('\r').Split('\t');
                    if (values.Length <= keyColumnIndex)
                        continue;

                    if (!int.TryParse(values[keyColumnIndex], out int rowKey))
                        continue;

                    if (rowKey != keyValue)
                        continue;

                    lines[i] = serializedRow;
                    replaced = true;
                    break;
                }

                if (!replaced)
                    lines.Add(serializedRow);

                string finalContent = string.Join(newline, lines);
                File.WriteAllText(fullPath, finalContent, new UTF8Encoding(false));

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception e)
            {
                error = $"테이블 행 저장 중 오류: {e.Message}";
                return false;
            }
        }

        private static List<string> SplitLines(string content)
        {
            return content
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .ToList();
        }

        private static string DetectNewline(string content)
        {
            return content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        }
    }
}
