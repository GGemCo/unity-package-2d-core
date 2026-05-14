using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 화면 페이드 요청이 이미 재생 중인 페이드를 만났을 때의 처리 방식을 정의합니다.
    /// </summary>
    [Serializable]
    public enum ScreenFadeReplaceMode
    {
        /// <summary>
        /// 현재 재생 중인 페이드를 중단하고 새 요청으로 교체합니다.
        /// 단, 현재 소유자의 우선순위가 더 높으면 교체하지 않습니다.
        /// </summary>
        ReplaceCurrent = 0,

        /// <summary>
        /// 현재 페이드가 재생 중이면 새 요청을 무시합니다.
        /// </summary>
        IgnoreIfPlaying = 1,

        /// <summary>
        /// 현재 소유자의 우선순위가 더 높거나 같으면 새 요청을 무시합니다.
        /// </summary>
        IgnoreIfOwnerPriorityIsGreaterOrEqual = 2,
    }
}
