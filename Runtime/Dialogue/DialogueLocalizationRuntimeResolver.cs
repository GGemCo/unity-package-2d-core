using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대사 노드와 선택지의 Localization table/key 를 런타임 문자열로 해석하는 도우미입니다.
    /// </summary>
    public static class DialogueLocalizationRuntimeResolver
    {
        private const string EscapedWindowsLineBreak = "\\r\\n";
        private const string EscapedLineFeed = "\\n";
        private const string EscapedCarriageReturn = "\\r";

        /// <summary>
        /// table/key 를 기준으로 문자열을 해석하고, 실패하면 fallback 문자열을 반환합니다.
        /// 최종 문자열에 포함된 대화 전용 줄바꿈 이스케이프는 실제 줄바꿈으로 변환합니다.
        /// </summary>
        /// <param name="table">String Table Collection 이름입니다.</param>
        /// <param name="key">String Table Entry Key 입니다.</param>
        /// <param name="fallback">로컬라이즈 실패 시 사용할 fallback 문자열입니다.</param>
        /// <param name="arguments">Smart String 평가에 사용할 인자입니다.</param>
        /// <returns>최종 출력 문자열입니다.</returns>
        public static string Resolve(string table, string key, string fallback, params object[] arguments)
        {
            string resolved = fallback ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(table) && !string.IsNullOrWhiteSpace(key))
            {
                LocalizationManager localizationManager = LocalizationManager.Instance;
                if (localizationManager != null)
                {
                    string localized = localizationManager.GetSmartString(table, key, arguments ?? Array.Empty<object>());
                    if (!string.IsNullOrWhiteSpace(localized))
                    {
                        resolved = localized;
                    }
                }
            }

            return DecodeLineBreaks(resolved);
        }

        /// <summary>
        /// 대화 문자열의 <c>\n</c>, <c>\r\n</c>, <c>\r</c> 표기를 실제 줄바꿈 문자로 변환합니다.
        /// 일반 Localization 문자열에는 영향을 주지 않도록 대화 해석 경로에서만 호출합니다.
        /// </summary>
        /// <param name="text">Localization 또는 fallback에서 해석된 최종 대화 문자열입니다.</param>
        /// <returns>줄바꿈 표기가 변환된 대화 문자열입니다.</returns>
        public static string DecodeLineBreaks(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            // 이스케이프 표기가 없는 대부분의 대사에서는 새 문자열을 만들지 않습니다.
            if (text.IndexOf(EscapedLineFeed, StringComparison.Ordinal) < 0 &&
                text.IndexOf(EscapedCarriageReturn, StringComparison.Ordinal) < 0)
            {
                return text;
            }

            // Windows 표기를 먼저 처리하여 \r\n 하나가 줄바꿈 두 개로 확장되지 않도록 합니다.
            return text
                .Replace(EscapedWindowsLineBreak, "\n")
                .Replace(EscapedLineFeed, "\n")
                .Replace(EscapedCarriageReturn, "\n");
        }

        /// <summary>
        /// 대사 노드 본문을 해석합니다.
        /// </summary>
        /// <param name="node">대사 노드 데이터입니다.</param>
        /// <param name="arguments">Smart String 평가 인자입니다.</param>
        /// <returns>표시할 대사 문자열입니다.</returns>
        public static string ResolveNodeText(DialogueNodeData node, params object[] arguments)
        {
            if (node == null)
            {
                return string.Empty;
            }

            return Resolve(node.dialogueTable, node.dialogueKey, node.dialogueText, arguments);
        }

        /// <summary>
        /// 선택지 문자열을 해석합니다.
        /// </summary>
        /// <param name="option">선택지 데이터입니다.</param>
        /// <param name="arguments">Smart String 평가 인자입니다.</param>
        /// <returns>표시할 선택지 문자열입니다.</returns>
        public static string ResolveOptionText(DialogueOption option, params object[] arguments)
        {
            if (option == null)
            {
                return string.Empty;
            }

            return Resolve(option.optionTable, option.optionKey, option.optionText, arguments);
        }
    }
}
