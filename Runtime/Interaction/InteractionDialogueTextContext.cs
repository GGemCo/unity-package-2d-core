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
    }
}
