using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대사 노드와 선택지의 Localization table/key 를 런타임 문자열로 해석하는 도우미입니다.
    /// </summary>
    public static class DialogueLocalizationRuntimeResolver
    {
        /// <summary>
        /// table/key 를 기준으로 문자열을 해석하고, 실패하면 fallback 문자열을 반환합니다.
        /// </summary>
        /// <param name="table">String Table Collection 이름입니다.</param>
        /// <param name="key">String Table Entry Key 입니다.</param>
        /// <param name="fallback">로컬라이즈 실패 시 사용할 fallback 문자열입니다.</param>
        /// <param name="arguments">Smart String 평가에 사용할 인자입니다.</param>
        /// <returns>최종 출력 문자열입니다.</returns>
        public static string Resolve(string table, string key, string fallback, params object[] arguments)
        {
            if (!string.IsNullOrWhiteSpace(table) && !string.IsNullOrWhiteSpace(key))
            {
                LocalizationManager localizationManager = LocalizationManager.Instance;
                if (localizationManager != null)
                {
                    string localized = localizationManager.GetSmartString(table, key, arguments ?? Array.Empty<object>());
                    if (!string.IsNullOrWhiteSpace(localized))
                    {
                        return localized;
                    }
                }
            }

            return fallback ?? string.Empty;
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
