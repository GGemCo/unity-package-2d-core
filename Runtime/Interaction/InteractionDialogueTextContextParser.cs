using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// NPC 테이블에 저장된 인터랙션 파라미터 문자열을 텍스트 컨텍스트로 변환합니다.
    /// </summary>
    public static class InteractionDialogueTextContextParser
    {
        private static readonly char[] Separators = { '|', '\n', '\r' };

        /// <summary>
        /// 구분자 기반 문자열을 위치 기반 파라미터 컨텍스트로 변환합니다.
        /// </summary>
        /// <param name="raw">테이블에 저장된 원본 파라미터 문자열입니다.</param>
        /// <returns>변환된 대사 텍스트 컨텍스트입니다.</returns>
        public static InteractionDialogueTextContext Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return InteractionDialogueTextContext.Empty;
            }

            string[] parts = raw.Split(Separators, StringSplitOptions.None);
            List<object> arguments = new List<object>();
            foreach (string part in parts)
            {
                string normalized = NormalizeValue(part);
                if (normalized == null)
                {
                    continue;
                }

                arguments.Add(ParseValue(normalized));
            }

            return arguments.Count > 0
                ? InteractionDialogueTextContext.FromArgs(arguments.ToArray())
                : InteractionDialogueTextContext.Empty;
        }

        /// <summary>
        /// 원본 파라미터 조각을 정규화합니다.
        /// </summary>
        /// <param name="value">원본 문자열 조각입니다.</param>
        /// <returns>정규화된 문자열입니다. 무시할 값이면 null 입니다.</returns>
        private static string NormalizeValue(string value)
        {
            if (value == null)
            {
                return null;
            }

            string normalized = value.Trim();
            if (normalized.Length == 0)
            {
                return string.Empty;
            }

            if (string.Equals(normalized, "None", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return normalized;
        }

        /// <summary>
        /// 문자열 값을 적절한 런타임 타입으로 변환합니다.
        /// </summary>
        /// <param name="value">정규화된 문자열 값입니다.</param>
        /// <returns>숫자, 불리언 또는 문자열 값입니다.</returns>
        private static object ParseValue(string value)
        {
            if (long.TryParse(value, out long longValue))
            {
                return longValue;
            }

            if (float.TryParse(value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float floatValue))
            {
                return floatValue;
            }

            if (TryParseBoolean(value, out bool boolValue))
            {
                return boolValue;
            }

            return value;
        }

        /// <summary>
        /// 문자열을 불리언 값으로 변환합니다.
        /// </summary>
        /// <param name="value">원본 문자열입니다.</param>
        /// <param name="result">변환된 결과입니다.</param>
        /// <returns>변환 성공 여부입니다.</returns>
        private static bool TryParseBoolean(string value, out bool result)
        {
            if (string.Equals(value, "Y", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "True", StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            if (string.Equals(value, "N", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "False", StringComparison.OrdinalIgnoreCase))
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }
    }
}
