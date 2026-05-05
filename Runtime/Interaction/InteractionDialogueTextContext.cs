using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인터랙션 대사 문자열 포맷에 사용할 컨텍스트입니다.
    /// </summary>
    public sealed class InteractionDialogueTextContext
    {
        /// <summary>
        /// 비어 있는 텍스트 컨텍스트입니다.
        /// </summary>
        public static readonly InteractionDialogueTextContext Empty = new InteractionDialogueTextContext(Array.Empty<object>());

        /// <summary>
        /// string.Format 위치 기반 치환에 사용할 인자 목록입니다.
        /// </summary>
        public object[] PositionalArgs { get; }

        /// <summary>
        /// 텍스트 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="positionalArgs">위치 기반 치환에 사용할 인자 목록입니다.</param>
        public InteractionDialogueTextContext(object[] positionalArgs)
        {
            PositionalArgs = positionalArgs ?? Array.Empty<object>();
        }

        /// <summary>
        /// 위치 기반 파라미터 배열로 텍스트 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="positionalArgs">위치 기반 치환에 사용할 인자 목록입니다.</param>
        /// <returns>생성된 텍스트 컨텍스트입니다.</returns>
        public static InteractionDialogueTextContext FromArgs(params object[] positionalArgs)
        {
            if (positionalArgs == null || positionalArgs.Length == 0)
            {
                return Empty;
            }

            object[] clonedArgs = new object[positionalArgs.Length];
            Array.Copy(positionalArgs, clonedArgs, positionalArgs.Length);
            return new InteractionDialogueTextContext(clonedArgs);
        }

        /// <summary>
        /// 기본 텍스트 컨텍스트와 추가 텍스트 컨텍스트를 순서대로 병합합니다.
        /// </summary>
        /// <param name="baseContext">앞쪽에 유지할 기본 텍스트 컨텍스트입니다.</param>
        /// <param name="additionalContext">뒤쪽에 추가할 텍스트 컨텍스트입니다.</param>
        /// <returns>병합된 텍스트 컨텍스트입니다.</returns>
        public static InteractionDialogueTextContext Merge(
            InteractionDialogueTextContext baseContext,
            InteractionDialogueTextContext additionalContext)
        {
            object[] baseArgs = baseContext?.PositionalArgs ?? Array.Empty<object>();
            object[] additionalArgs = additionalContext?.PositionalArgs ?? Array.Empty<object>();
            if (baseArgs.Length == 0)
            {
                return additionalArgs.Length == 0 ? Empty : FromArgs(additionalArgs);
            }

            if (additionalArgs.Length == 0)
            {
                return FromArgs(baseArgs);
            }

            object[] mergedArgs = new object[baseArgs.Length + additionalArgs.Length];
            Array.Copy(baseArgs, mergedArgs, baseArgs.Length);
            Array.Copy(additionalArgs, 0, mergedArgs, baseArgs.Length, additionalArgs.Length);
            return new InteractionDialogueTextContext(mergedArgs);
        }
    }
}
