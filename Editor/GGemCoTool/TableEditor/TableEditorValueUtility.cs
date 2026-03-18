using System;
using System.Collections;
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

            if (targetType == typeof(bool))
            {
                value = string.Equals(raw, "Y", StringComparison.OrdinalIgnoreCase) || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
                return true;
            }

            if (targetType == typeof(Vector2))
            {
                string[] tokens = raw.Split(',');
                if (tokens.Length == 0 || string.IsNullOrWhiteSpace(raw))
                {
                    value = Vector2.zero;
                    return true;
                }

                if (TryParseFloat(tokens.ElementAtOrDefault(0), out float x) && TryParseFloat(tokens.ElementAtOrDefault(1), out float y))
                {
                    value = new Vector2(x, y);
                    return true;
                }

                error = "Vector2 형식 오류 (x,y)";
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
            if (sourceType == null || sourceType == typeof(string))
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

                    row.Values.TryGetValue(document.Headers[i], out string value);
                    builder.Append((value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", "\\n"));
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

        private static string NormalizeNumeric(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? "0" : raw.Trim();
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            return float.TryParse(NormalizeNumeric(raw), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
