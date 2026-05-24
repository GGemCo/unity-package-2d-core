using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 화면 전체 페이드 연출을 요청한 시스템의 소유자를 정의합니다.
    /// 전역 화면 연출이 서로 충돌하지 않도록 우선순위 판단에 사용합니다.
    /// </summary>
    [Serializable]
    public enum ScreenFadeOwner
    {
        /// <summary>
        /// 소유자가 지정되지 않은 기본 상태입니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 스킬 연출에서 요청한 화면 페이드입니다.
        /// </summary>
        Skill = 10,

        /// <summary>
        /// UI 윈도우 전환에서 요청한 화면 페이드입니다.
        /// 스킬보다 높고 컷신보다 낮은 우선순위를 가집니다.
        /// </summary>
        UiWindow = 20,

        /// <summary>
        /// 맵 종료 정책에서 요청한 화면 페이드입니다.
        /// UI 윈도우보다 높고 컷신보다 낮은 우선순위를 가집니다.
        /// </summary>
        MapExit = 30,

        /// <summary>
        /// 컷신 연출에서 요청한 화면 페이드입니다.
        /// UI 윈도우와 스킬보다 높은 우선순위를 가집니다.
        /// </summary>
        Cutscene = 100,
    }
}
