using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal static class TableEditorValueUtility
    {
        public static bool TryConvertFromRaw(string raw, Type targetType, out object value, out string error)
        {
            error = null;
            value = null;

            if (targetType == null)
            {
                value = raw ?? string.Empty;
                return true;
            }

            raw ??= string.Empty;
            targetType = UnwrapNullable(targetType);

            if (targetType == typeof(string))
            {
                value = raw;
                return true;
            }

            if (targetType == typeof(int))
            {
                if (int.TryParse(NormalizeNumeric(raw), NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                {
                    value = intValue;
                    return true;
                }

                error = "int 파싱 실패";
                return false;
            }

            if (targetType == typeof(long))
            {
                if (long.TryParse(NormalizeNumeric(raw), NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
                {
                    value = longValue;
                    return true;
                }

                error = "long 파싱 실패";
                return false;
            }

            if (targetType == typeof(float))
            {
                if (float.TryParse(NormalizeNumeric(raw), NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                {
                    value = floatValue;
                    return true;
                }

                error = "float 파싱 실패";
                return false;
            }

            if (targetType == typeof(double))
            {
                if (double.TryParse(NormalizeNumeric(raw), NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
                {
                    value = doubleValue;
                    return true;
                }

                error = "double 파싱 실패";
                return false;
            }

            if (targetType == typeof(bool))
            {
                value = string.Equals(raw, "Y", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase);
                return true;
            }

            if (targetType == typeof(Vector2))
            {
                if (TryParseVector(raw, 2, out float[] values2))
                {
                    value = new Vector2(values2[0], values2[1]);
                    return true;
                }

                error = "Vector2 형식 오류 (x,y)";
                return false;
            }

            if (targetType == typeof(Vector3))
            {
                if (TryParseVector(raw, 3, out float[] values3))
                {
                    value = new Vector3(values3[0], values3[1], values3[2]);
                    return true;
                }

                error = "Vector3 형식 오류 (x,y,z)";
                return false;
            }

            if (targetType == typeof(Vector4))
            {
                if (TryParseVector(raw, 4, out float[] values4))
                {
                    value = new Vector4(values4[0], values4[1], values4[2], values4[3]);
                    return true;
                }

                error = "Vector4 형식 오류 (x,y,z,w)";
                return false;
            }

            if (targetType == typeof(Color))
            {
                if (TryParseColor(raw, out Color color))
                {
                    value = color;
                    return true;
                }

                error = "Color 파싱 실패 (#RRGGBB / #RRGGBBAA)";
                return false;
            }

            if (targetType == typeof(Color32))
            {
                if (TryParseColor(raw, out Color32 color32))
                {
                    value = color32;
                    return true;
                }

                error = "Color32 파싱 실패 (#RRGGBB / #RRGGBBAA)";
                return false;
            }

            if (targetType == typeof(Vector2[]))
            {
                if (TryParseVector2Array(raw, out Vector2[] vectors, out string vectorArrayError))
                {
                    value = vectors;
                    return true;
                }

                error = vectorArrayError;
                return false;
            }

            if (targetType == typeof(ProjectileMoveSegment[]))
            {
                if (TryParseProjectileMoveSegmentArray(raw, out ProjectileMoveSegment[] segments, out string segmentArrayError))
                {
                    value = segments;
                    return true;
                }

                error = segmentArrayError;
                return false;
            }

            if (targetType.IsArray)
            {
                Type elementType = targetType.GetElementType();
                string[] tokens = string.IsNullOrWhiteSpace(raw)
                    ? Array.Empty<string>()
                    : raw.Split(',').Select(static t => t.Trim()).ToArray();

                Array array = Array.CreateInstance(elementType, tokens.Length);
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (!TryConvertFromRaw(tokens[i], elementType, out object elementValue, out string elementError))
                    {
                        error = $"배열 원소 파싱 실패: {elementError}";
                        return false;
                    }

                    if (elementValue == null && elementType.IsValueType)
                    {
                        elementValue = Activator.CreateInstance(elementType);
                    }

                    array.SetValue(elementValue, i);
                }

                value = array;
                return true;
            }

            if (targetType.IsEnum)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    Array values = Enum.GetValues(targetType);
                    value = values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(targetType);
                    return true;
                }

                try
                {
                    value = Enum.Parse(targetType, raw, true);
                    return true;
                }
                catch
                {
                    error = $"Enum 파싱 실패: {targetType.Name}";
                    return false;
                }
            }

            value = raw;
            return true;
        }

        public static string ConvertToRaw(object value, Type sourceType)
        {
            if (sourceType == null)
                return value?.ToString() ?? string.Empty;

            sourceType = UnwrapNullable(sourceType);
            if (sourceType == typeof(string))
                return value?.ToString() ?? string.Empty;

            if (sourceType == typeof(bool))
                return value is bool b && b ? "Y" : "N";

            if (sourceType == typeof(float))
                return Convert.ToSingle(value ?? 0f, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

            if (sourceType == typeof(double))
                return Convert.ToDouble(value ?? 0d, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

            if (sourceType == typeof(Vector2))
            {
                Vector2 v = value is Vector2 vector ? vector : Vector2.zero;
                return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)}";
            }

            if (sourceType == typeof(Vector3))
            {
                Vector3 v = value is Vector3 vector ? vector : Vector3.zero;
                return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)}";
            }

            if (sourceType == typeof(Vector4))
            {
                Vector4 v = value is Vector4 vector ? vector : Vector4.zero;
                return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)},{v.w.ToString(CultureInfo.InvariantCulture)}";
            }

            if (sourceType == typeof(Color))
            {
                Color color = value is Color c ? c : Color.white;
                return ColorUtility.ToHtmlStringRGBA(color);
            }

            if (sourceType == typeof(Color32))
            {
                Color32 color = value is Color32 c ? c : new Color32(255, 255, 255, 255);
                return ColorUtility.ToHtmlStringRGBA(color);
            }

            if (sourceType == typeof(Vector2[]))
            {
                return ConvertVector2ArrayToRaw(value as IReadOnlyList<Vector2>);
            }

            if (sourceType == typeof(ProjectileMoveSegment[]))
            {
                return ConvertProjectileMoveSegmentArrayToRaw(value as IReadOnlyList<ProjectileMoveSegment>);
            }

            if (sourceType.IsArray)
            {
                Array array = value as Array;
                if (array == null || array.Length == 0)
                    return string.Empty;

                Type elementType = sourceType.GetElementType();
                List<string> tokens = new List<string>(array.Length);
                foreach (object element in array)
                    tokens.Add(ConvertToRaw(element, elementType));
                return string.Join(",", tokens);
            }

            if (sourceType.IsEnum)
                return value != null ? value.ToString() : string.Empty;

            if (sourceType == typeof(int) || sourceType == typeof(long) || sourceType == typeof(short) || sourceType == typeof(byte))
                return Convert.ToString(value ?? 0, CultureInfo.InvariantCulture);

            return value?.ToString() ?? string.Empty;
        }

        public static object BuildRowObject(TableEditorTableDefinition definition, TableEditorDocumentRow row, out List<string> fieldErrors)
        {
            fieldErrors = new List<string>();
            if (definition == null || row == null)
                return null;

            object instance = Activator.CreateInstance(definition.RowType);
            foreach (MemberInfo member in TableEditorReflectionUtility.GetEditableMembers(definition.RowType))
            {
                Type memberType = TableEditorReflectionUtility.GetMemberType(member);
                row.Values.TryGetValue(member.Name, out string raw);
                if (!TryConvertFromRaw(raw, memberType, out object converted, out string error))
                {
                    fieldErrors.Add($"{member.Name}: {error}");
                    continue;
                }

                TableEditorReflectionUtility.SetValue(instance, member, converted);
            }

            return instance;
        }

        public static string BuildTempContent(TableEditorDocument document)
        {
            if (document == null)
                return string.Empty;

            System.Text.StringBuilder builder = new System.Text.StringBuilder(4096);
            builder.Append(string.Join("\t", document.Headers));
            builder.Append('\n');

            foreach (TableEditorDocumentRow row in document.GetRows())
            {
                for (int i = 0; i < document.Headers.Count; i++)
                {
                    if (i > 0)
                        builder.Append('\t');

                    row.Values.TryGetValue(document.Headers[i], out string itemValue);
                    builder.Append((itemValue ?? string.Empty).Replace("\r", string.Empty).Replace("\n", "\\n"));
                }

                builder.Append('\n');
            }

            return builder.ToString();
        }

        public static string GetRowUidRaw(TableEditorDocumentRow row)
        {
            if (row == null)
                return string.Empty;

            return row.Values.TryGetValue("Uid", out string value) ? value : string.Empty;
        }

        private static Type UnwrapNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        private static string NormalizeNumeric(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? "0" : raw.Trim();
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            return float.TryParse(NormalizeNumeric(raw), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseVector(string raw, int expectedCount, out float[] values)
        {
            values = new float[expectedCount];
            if (string.IsNullOrWhiteSpace(raw))
                return true;

            string[] tokens = raw.Split(',');
            if (tokens.Length < expectedCount)
                return false;

            for (int i = 0; i < expectedCount; i++)
            {
                if (!TryParseFloat(tokens[i], out values[i]))
                    return false;
            }

            return true;
        }

        private static bool TryParseColor(string raw, out Color value)
        {
            string normalized = NormalizeColor(raw);
            if (ColorUtility.TryParseHtmlString(normalized, out Color parsed))
            {
                value = parsed;
                return true;
            }

            value = Color.white;
            return false;
        }

        private static bool TryParseColor(string raw, out Color32 value)
        {
            if (TryParseColor(raw, out Color parsed))
            {
                value = (Color32)parsed;
                return true;
            }

            value = new Color32(255, 255, 255, 255);
            return false;
        }

        /// <summary>
        /// Table Editor에서 사용하는 PathPoints 형식의 문자열을 Vector2 배열로 변환합니다.
        /// - 각 점은 "x,y" 형식입니다.
        /// - 여러 점은 "|" 또는 ";" 로 구분합니다.
        /// </summary>
        /// <param name="raw">파싱할 원본 문자열입니다.</param>
        /// <param name="values">파싱된 Vector2 배열입니다.</param>
        /// <param name="error">파싱 실패 시 오류 메시지입니다.</param>
        /// <returns>파싱 성공 여부입니다.</returns>
        private static bool TryParseVector2Array(string raw, out Vector2[] values, out string error)
        {
            values = Array.Empty<Vector2>();
            error = null;

            if (string.IsNullOrWhiteSpace(raw))
                return true;

            string[] tokens = raw.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            values = new Vector2[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!TryParseVector(tokens[i], 2, out float[] parsed))
                {
                    error = "Vector2 배열 형식 오류 (x,y|x,y)";
                    values = Array.Empty<Vector2>();
                    return false;
                }

                values[i] = new Vector2(parsed[0], parsed[1]);
            }

            return true;
        }

        /// <summary>
        /// Table Editor에서 사용하는 이동 세그먼트 문자열을 <see cref="ProjectileMoveSegment"/> 배열로 변환합니다.
        /// - 각 세그먼트는 "dirX,dirY,speed,distance" 형식입니다.
        /// - 여러 세그먼트는 "|" 또는 ";" 로 구분합니다.
        /// </summary>
        /// <param name="raw">파싱할 원본 문자열입니다.</param>
        /// <param name="values">파싱된 이동 세그먼트 배열입니다.</param>
        /// <param name="error">파싱 실패 시 오류 메시지입니다.</param>
        /// <returns>파싱 성공 여부입니다.</returns>
        private static bool TryParseProjectileMoveSegmentArray(string raw, out ProjectileMoveSegment[] values, out string error)
        {
            values = Array.Empty<ProjectileMoveSegment>();
            error = null;

            if (string.IsNullOrWhiteSpace(raw))
                return true;

            string[] tokens = raw.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            values = new ProjectileMoveSegment[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!TryParseProjectileMoveSegment(tokens[i], out ProjectileMoveSegment segment))
                {
                    error = "MoveSegments 형식 오류 (dirX,dirY,speed,distance|...)";
                    values = Array.Empty<ProjectileMoveSegment>();
                    return false;
                }

                values[i] = segment;
            }

            return true;
        }

        /// <summary>
        /// "dirX,dirY,speed,distance" 형식의 문자열을 이동 세그먼트 값으로 변환합니다.
        /// </summary>
        /// <param name="raw">파싱할 세그먼트 문자열입니다.</param>
        /// <param name="value">파싱된 이동 세그먼트입니다.</param>
        /// <returns>파싱 성공 여부입니다.</returns>
        private static bool TryParseProjectileMoveSegment(string raw, out ProjectileMoveSegment value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(raw))
                return true;

            string[] parts = raw.Split(',');
            if (!TryParseFloat(parts.Length > 0 ? parts[0] : "0", out float dirX)
                || !TryParseFloat(parts.Length > 1 ? parts[1] : "0", out float dirY)
                || !TryParseFloat(parts.Length > 2 ? parts[2] : "0", out float speed)
                || !TryParseFloat(parts.Length > 3 ? parts[3] : "0", out float distance))
            {
                return false;
            }

            value = new ProjectileMoveSegment(new Vector2(dirX, dirY), speed, distance);
            return true;
        }

        /// <summary>
        /// Vector2 배열을 Table Editor 저장 문자열 형식으로 변환합니다.
        /// - 각 점은 "x,y" 형식입니다.
        /// - 여러 점은 "|" 로 연결합니다.
        /// </summary>
        /// <param name="values">변환할 Vector2 배열입니다.</param>
        /// <returns>저장 가능한 원본 문자열입니다.</returns>
        private static string ConvertVector2ArrayToRaw(IReadOnlyList<Vector2> values)
        {
            if (values == null || values.Count == 0)
                return string.Empty;

            List<string> tokens = new List<string>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                Vector2 item = values[i];
                tokens.Add($"{item.x.ToString(CultureInfo.InvariantCulture)},{item.y.ToString(CultureInfo.InvariantCulture)}");
            }

            return string.Join("|", tokens);
        }

        /// <summary>
        /// 이동 세그먼트 배열을 Table Editor 저장 문자열 형식으로 변환합니다.
        /// - 각 세그먼트는 "dirX,dirY,speed,distance" 형식입니다.
        /// - 여러 세그먼트는 "|" 로 연결합니다.
        /// </summary>
        /// <param name="values">변환할 이동 세그먼트 배열입니다.</param>
        /// <returns>저장 가능한 원본 문자열입니다.</returns>
        private static string ConvertProjectileMoveSegmentArrayToRaw(IReadOnlyList<ProjectileMoveSegment> values)
        {
            if (values == null || values.Count == 0)
                return string.Empty;

            List<string> tokens = new List<string>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                ProjectileMoveSegment item = values[i];
                tokens.Add(string.Join(",",
                    item.Direction.x.ToString(CultureInfo.InvariantCulture),
                    item.Direction.y.ToString(CultureInfo.InvariantCulture),
                    item.Speed.ToString(CultureInfo.InvariantCulture),
                    item.Distance.ToString(CultureInfo.InvariantCulture)));
            }

            return string.Join("|", tokens);
        }

        private static string NormalizeColor(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "#FFFFFFFF";

            string normalized = raw.Trim();
            if (!normalized.StartsWith("#", StringComparison.Ordinal))
                normalized = "#" + normalized;

            return normalized;
        }
    }
}
