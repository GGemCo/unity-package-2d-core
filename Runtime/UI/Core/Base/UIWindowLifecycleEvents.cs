using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// Core UI 윈도우의 표시 상태 변화를 외부 시스템에 전달하는 공용 이벤트 허브입니다.
    /// 상위 패키지는 이 이벤트를 구독한 뒤 자신이 필요한 이벤트 버스로 변환합니다.
    /// </summary>
    public static class UIWindowLifecycleEvents
    {
        /// <summary>
        /// UIWindowBase.OnShow가 호출되어 윈도우 표시 상태가 변경될 때 발생합니다.
        /// </summary>
        public static event Action<UIWindowBase, bool> VisibilityChanged;

        /// <summary>
        /// 지정한 윈도우의 표시 상태 변경을 발행합니다.
        /// </summary>
        /// <param name="window">표시 상태가 변경된 윈도우입니다.</param>
        /// <param name="show">true면 열림, false면 닫힘 상태입니다.</param>
        public static void PublishVisibilityChanged(UIWindowBase window, bool show)
        {
            if (window == null)
            {
                return;
            }

            VisibilityChanged?.Invoke(window, show);
        }

        /// <summary>
        /// 도메인 재로드 또는 플레이 모드 재시작 시 이전 구독 상태를 정리합니다.
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            VisibilityChanged = null;
        }
    }
}
