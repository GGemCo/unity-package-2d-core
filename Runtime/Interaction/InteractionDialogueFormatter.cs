using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인터랙션 대사 문자열 포맷을 담당하는 유틸리티입니다.
    /// </summary>
    public static class InteractionDialogueFormatter
    {
        /// <summary>
        /// 원본 문자열에 위치 기반 파라미터를 적용합니다.
        /// 포맷 문자열이 잘못되었으면 원본을 그대로 반환합니다.
        /// </summary>
        /// <param name="template">원본 문자열입니다.</param>
        /// <param name="context">대사 텍스트 컨텍스트입니다.</param>
        /// <returns>포맷이 적용된 문자열입니다.</returns>
        public static string FormatRaw(string template, InteractionDialogueTextContext context)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            object[] args = context?.PositionalArgs ?? Array.Empty<object>();
            if (args.Length == 0)
            {
                return template;
            }

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException exception)
            {
                GcLogger.LogError($"interaction dialogue 포맷 문자열 오류: {exception.Message}, template: {template}");
                return template;
            }
        }
    }
}
