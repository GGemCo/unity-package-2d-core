using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 윈도우 표시/숨김 연출을 담당하는 컴포넌트입니다.
    /// 기존 CanvasGroup Fade 흐름을 유지하면서 공용 UI 효과 서비스와 연결합니다.
    /// </summary>
    public class UIWindowFade : MonoBehaviour
    {
        private UIWindow uiWindow;

        private void Awake()
        {
            uiWindow = GetComponent<UIWindow>();
            UiFadeUtility.TryGetCanvasGroup(gameObject, true, out _);
            UIEffectTarget.GetOrAdd(gameObject);
        }

        /// <summary>
        /// window 열기
        /// </summary>
        public void ShowPanel()
        {
            if (uiWindow != null && uiWindow.gameObject.activeSelf)
                return;

            uiWindow?.OnShow(true);
            UIEffectService.PlayWindow(this, gameObject, true);
        }

        /// <summary>
        /// window 닫기
        /// </summary>
        public void HidePanel()
        {
            if (uiWindow == null || !uiWindow.gameObject.activeSelf)
                return;

            UIEffectService.PlayWindow(this, gameObject, false, OnWindowClosed);
        }

        private void OnWindowClosed(bool show)
        {
            if (show)
                return;

            uiWindow?.OnShow(false);
        }
    }
}
