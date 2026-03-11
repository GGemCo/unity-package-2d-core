using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 윈도우 표시/숨김 연출을 담당하는 컴포넌트입니다.
    /// </summary>
    public class UIWindowFade : MonoBehaviour
    {
        private UIWindowBase _uiWindowBase;
        private bool _isTransitioning;

        private void Awake()
        {
            _uiWindowBase = GetComponent<UIWindowBase>();
            UiFadeUtility.TryGetCanvasGroup(gameObject, true, out _);
            UIEffectTarget.GetOrAdd(gameObject);
        }

        /// <summary>
        /// window 열기
        /// </summary>
        public void ShowPanel()
        {
            if (_isTransitioning)
                return;

            if (gameObject.activeSelf)
                return;

            _isTransitioning = true;
            gameObject.SetActive(true);
            _uiWindowBase?.OnShow(true);

            UIEffectService.PlayWindow(
                this,
                gameObject,
                true,
                _uiWindowBase != null ? _uiWindowBase.WindowOpenPreset : null,
                _uiWindowBase != null ? _uiWindowBase.WindowClosePreset : null,
                _ => { _isTransitioning = false; });
        }

        /// <summary>
        /// window 닫기
        /// </summary>
        public void HidePanel()
        {
            if (_isTransitioning)
                return;

            if (!gameObject.activeSelf)
                return;

            _isTransitioning = true;
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            UIEffectService.PlayWindow(
                this,
                gameObject,
                false,
                _uiWindowBase != null ? _uiWindowBase.WindowOpenPreset : null,
                _uiWindowBase != null ? _uiWindowBase.WindowClosePreset : null,
                OnWindowClosed);
        }

        private void OnWindowClosed(bool show)
        {
            _isTransitioning = false;
            if (show)
                return;

            _uiWindowBase?.OnShow(false);
            gameObject.SetActive(false);
        }
    }
}
