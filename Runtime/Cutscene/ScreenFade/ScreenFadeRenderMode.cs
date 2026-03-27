using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// Screen Fade 렌더링 방식을 정의합니다.
    /// </summary>
    [Serializable]
    public enum ScreenFadeRenderMode
    {
        /// <summary>
        /// 기존 UI Overlay Canvas 위에 렌더링합니다.
        /// 월드 캐릭터보다 항상 위에 표시됩니다.
        /// </summary>
        OverlayUi = 0,

        /// <summary>
        /// Screen Space - Camera Canvas 로 렌더링합니다.
        /// Sorting Layer / Order in Layer 로 월드 오브젝트와의 전후 관계를 제어할 수 있습니다.
        /// </summary>
        ScreenSpaceCamera = 1,
    }
}
