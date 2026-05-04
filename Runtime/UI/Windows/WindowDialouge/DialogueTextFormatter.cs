using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 메시지를 maxLineCount 씩 자릅니다.
    /// </summary>
    public static class DialogueTextFormatter
    {
        /// <summary>
        /// 입력 메시지를 지정한 줄 수 기준으로 페이지 단위 문자열 목록으로 분할합니다.
        /// </summary>
        /// <param name="message">분할할 원본 메시지입니다.</param>
        /// <param name="maxLineCount">페이지당 최대 줄 수입니다.</param>
        /// <returns>분할된 메시지 페이지 목록입니다.</returns>
        public static List<string> SplitMessage(string message, int maxLineCount)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(message))
            {
                return result;
            }

            int safeMaxLineCount = Mathf.Max(1, maxLineCount);
            string[] lines = message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i += safeMaxLineCount)
            {
                string chunk = string.Join("\n", lines, i, Math.Min(safeMaxLineCount, lines.Length - i));
                result.Add(chunk);
            }

            if (result.Count == 0)
            {
                result.Add(message);
            }

            return result;
        }
    }
}
