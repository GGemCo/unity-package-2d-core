using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 테이블 한 행의 문자열 데이터를 강타입 값으로 읽기 위한 공용 파서입니다.
    /// </summary>
    public readonly struct TableRowReader
    {
        private readonly IReadOnlyDictionary<string, string> _data;
        private readonly string _tableName;

        /// <summary>
        /// 테이블 행 파서를 생성합니다.
        /// </summary>
        /// <param name="data">컬럼 이름과 문자열 값으로 구성된 행 데이터입니다.</param>
        /// <param name="tableName">오류 로그에 표시할 테이블 이름입니다.</param>
        public TableRowReader(IReadOnlyDictionary<string, string> data, string tableName = "")
        {
            _data = data;
            _tableName = tableName ?? string.Empty;
        }

        /// <summary>
        /// 지정한 컬럼이 현재 행에 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="columnName">확인할 컬럼 이름입니다.</param>
        /// <returns>컬럼이 존재하면 true, 아니면 false입니다.</returns>
        public bool HasColumn(string columnName)
        {
            return _data != null && !string.IsNullOrWhiteSpace(columnName) && _data.ContainsKey(columnName);
        }

        /// <summary>
        /// 지정한 컬럼의 원본 문자열 값이 비어 있는지 확인합니다.
        /// </summary>
        /// <param name="columnName">확인할 컬럼 이름입니다.</param>
        /// <returns>컬럼이 없거나 값이 비어 있으면 true입니다.</returns>
        public bool IsEmpty(string columnName)
        {
            return string.IsNullOrWhiteSpace(String(columnName));
        }

        /// <summary>
        /// 지정한 컬럼의 원본 문자열 값을 반환합니다.
        /// 컬럼이 없거나 값이 null이면 기본값을 반환합니다.
        /// </summary>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <param name="defaultValue">컬럼이 없거나 값이 비어 있을 때 사용할 기본값입니다.</param>
        /// <returns>컬럼의 문자열 값 또는 기본값입니다.</returns>
        public string String(string columnName, string defaultValue = "")
        {
            if (_data == null || string.IsNullOrWhiteSpace(columnName))
                return defaultValue;

            if (!_data.TryGetValue(columnName, out string value))
                return defaultValue;

            if (string.IsNullOrWhiteSpace(value) || IsNone(value))
                return defaultValue;

            return value;
        }

        /// <summary>
        /// 지정한 필수 컬럼의 문자열 값을 반환합니다.
        /// 컬럼이 없거나 값이 비어 있으면 테이블 오류를 명확히 표시하기 위해 예외를 발생시킵니다.
        /// </summary>
        /// <param name="columnName">읽을 필수 컬럼 이름입니다.</param>
        /// <returns>필수 컬럼의 문자열 값입니다.</returns>
        public string RequiredString(string columnName)
        {
            string value = String(columnName);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            throw new InvalidOperationException(
                $"[TableRowReader] 필수 컬럼 값이 없습니다. table={_tableName}, column={columnName}");
        }

        /// <summary>
        /// 지정한 컬럼을 정수 값으로 파싱합니다.
        /// </summary>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <param name="defaultValue">파싱 실패 시 사용할 기본값입니다.</param>
        /// <returns>파싱된 정수 값입니다.</returns>
        public int Int(string columnName, int defaultValue = 0)
        {
            return MathHelper.ParseInt(String(columnName), defaultValue);
        }

        /// <summary>
        /// 지정한 컬럼을 long 값으로 파싱합니다.
        /// </summary>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <param name="defaultValue">파싱 실패 시 사용할 기본값입니다.</param>
        /// <returns>파싱된 long 값입니다.</returns>
        public long Long(string columnName, long defaultValue = 0L)
        {
            return MathHelper.ParseLong(String(columnName), defaultValue);
        }

        /// <summary>
        /// 지정한 컬럼을 실수 값으로 파싱합니다.
        /// </summary>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <param name="defaultValue">파싱 실패 시 사용할 기본값입니다.</param>
        /// <returns>파싱된 실수 값입니다.</returns>
        public float Float(string columnName, float defaultValue = 0f)
        {
            return MathHelper.ParseFloat(String(columnName), defaultValue);
        }

        /// <summary>
        /// 지정한 컬럼을 Y/N 형식의 bool 값으로 파싱합니다.
        /// </summary>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <param name="defaultValue">컬럼이 비어 있을 때 사용할 기본값입니다.</param>
        /// <returns>값이 Y이면 true, N이거나 비어 있으면 false입니다.</returns>
        public bool BoolYN(string columnName, bool defaultValue = false)
        {
            string value = String(columnName);
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value == "Y";
        }


        /// <summary>
        /// 지정한 컬럼을 Y/N, true/false, 1/0 형식의 bool 값으로 느슨하게 파싱합니다.
        /// </summary>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <param name="defaultValue">컬럼이 비어 있을 때 사용할 기본값입니다.</param>
        /// <returns>true로 해석되는 값이면 true, false로 해석되는 값이면 false입니다.</returns>
        public bool BoolLoose(string columnName, bool defaultValue = false)
        {
            string value = String(columnName);
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            string trimmed = value.Trim();
            if (trimmed == "Y" || trimmed == "1" || string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "on", StringComparison.OrdinalIgnoreCase))
                return true;

            if (trimmed == "N" || trimmed == "0" || string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "no", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "off", StringComparison.OrdinalIgnoreCase))
                return false;

            return defaultValue;
        }

        /// <summary>
        /// 지정한 컬럼을 Enum 값으로 파싱합니다.
        /// </summary>
        /// <typeparam name="TEnum">변환할 Enum 타입입니다.</typeparam>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <param name="defaultValue">컬럼이 비어 있을 때 사용할 기본값입니다.</param>
        /// <returns>파싱된 Enum 값입니다.</returns>
        public TEnum Enum<TEnum>(string columnName, TEnum defaultValue = default) where TEnum : struct, Enum
        {
            string value = String(columnName);
            return string.IsNullOrWhiteSpace(value) ? defaultValue : EnumHelper.ConvertEnum<TEnum>(value);
        }

        /// <summary>
        /// 지정한 컬럼을 Vector2 값으로 파싱합니다.
        /// </summary>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <param name="defaultValue">컬럼이 비어 있거나 파싱할 수 없을 때 사용할 기본값입니다.</param>
        /// <returns>파싱된 Vector2 값입니다.</returns>
        public Vector2 Vector2(string columnName, Vector2 defaultValue = default)
        {
            string value = String(columnName);
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            string[] parts = value.Split(',');
            if (parts.Length == 0)
                return defaultValue;

            float x = MathHelper.ParseFloat(parts.Length > 0 ? parts[0] : "0", defaultValue.x);
            float y = MathHelper.ParseFloat(parts.Length > 1 ? parts[1] : "0", defaultValue.y);
            return new Vector2(x, y);
        }

        /// <summary>
        /// 지정한 컬럼을 콤마로 구분된 정수 배열로 파싱합니다.
        /// 0 또는 빈 값은 빈 배열로 처리합니다.
        /// </summary>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <returns>파싱된 정수 배열입니다.</returns>
        public int[] IntArray(string columnName)
        {
            string value = String(columnName);
            if (value == "0" || string.IsNullOrWhiteSpace(value))
                return Array.Empty<int>();

            string[] values = value.Split(',');
            int[] result = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
                result[i] = MathHelper.ParseInt(values[i]);

            return result;
        }

        /// <summary>
        /// 테이블에서 비어 있는 값으로 취급할 None 토큰인지 확인합니다.
        /// </summary>
        /// <param name="value">검사할 문자열 값입니다.</param>
        /// <returns>None 토큰이면 true입니다.</returns>
        private static bool IsNone(string value)
        {
            return string.Equals(value, "None", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 테이블 행에서 DamageType 컬럼을 읽어 <see cref="ConfigCommon.DamageType"/> 값으로 변환합니다.
        /// </summary>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <returns>파싱된 데미지 타입입니다. 값이 비어 있으면 물리 데미지를 반환합니다.</returns>
        public ConfigCommon.DamageType DamageType(string columnName)
        {
            string value = String(columnName);
            if (string.IsNullOrWhiteSpace(value))
                return ConfigCommon.DamageType.Physic;

            value = value.Trim();
            if (value.StartsWith("DT_", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(3);
            }

            if (int.TryParse(value, out int rawValue) &&
                System.Enum.IsDefined(typeof(ConfigCommon.DamageType), rawValue))
            {
                return (ConfigCommon.DamageType)rawValue;
            }

            return EnumHelper.ConvertEnum<ConfigCommon.DamageType>(value);
        }
    }
}
