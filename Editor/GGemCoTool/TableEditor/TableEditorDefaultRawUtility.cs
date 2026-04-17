using System;

namespace GGemCo2DCoreEditor
{
    internal static class TableEditorDefaultRawUtility
    {
        public static bool TryGetDefaultRaw(TableEditorColumnDefinition column, out string raw)
        {
            raw = string.Empty;
            if (column == null)
                return false;

            Type valueType = column.ValueType;
            if (valueType == null)
                return false;

            Type type = Nullable.GetUnderlyingType(valueType) ?? valueType;

            if (type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                object defaultValue = values.Length > 0
                    ? values.GetValue(0)
                    : Activator.CreateInstance(type);

                raw = TableEditorValueUtility.ConvertToRaw(defaultValue, type);
                return true;
            }

            if (type == typeof(bool))
            {
                raw = TableEditorValueUtility.ConvertToRaw(false, type);
                return true;
            }

            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            {
                raw = TableEditorValueUtility.ConvertToRaw(0, type);
                return true;
            }

            if (type == typeof(float))
            {
                raw = TableEditorValueUtility.ConvertToRaw(0f, type);
                return true;
            }

            if (type == typeof(double))
            {
                raw = TableEditorValueUtility.ConvertToRaw(0d, type);
                return true;
            }

            return false;
        }
    }
}
