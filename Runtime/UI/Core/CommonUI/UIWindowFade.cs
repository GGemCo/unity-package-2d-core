using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindowBase와 UIEffectPreset을 연결하는 윈도우 전환 브리지입니다.
    /// </summary>
    public class UIWindowFade : MonoBehaviour
    {
        private UIWindowBase _uiWindow;
        private UIEffectTarget _effectTarget;
        private UIEffectPreset _openPreset;
        private UIEffectPreset _closePreset;
        private bool _isTransitionRunning;

        private void Awake()
        {
            if (_uiWindow == null)
                _uiWindow = GetComponent<UIWindowBase>();

            if (_effectTarget == null)
                _effectTarget = UIEffectTarget.GetOrAdd(gameObject);
        }

        public void Initialize(UIWindowBase windowBase, UIEffectTarget effectTarget, UIEffectPreset openPreset, UIEffectPreset closePreset)
        {
            _uiWindow = windowBase;
            _effectTarget = effectTarget != null ? effectTarget : UIEffectTarget.GetOrAdd(gameObject);
            _openPreset = openPreset;
            _closePreset = closePreset;
        }

        /// <summary>
        /// window 열기
        /// </summary>
        public void ShowPanel()
        {
            if (_isTransitionRunning) return;
            if (gameObject.activeSelf) return;

            gameObject.SetActive(true);
            _uiWindow?.OnShow(true);

            if (_openPreset == null)
            {
                UiFadeUtility.SetVisible(gameObject, true, true, true);
                return;
            }

            _isTransitionRunning = true;
            UIEffectService.Play(this, _effectTarget, _openPreset, () => _isTransitionRunning = false);
        }

        /// <summary>
        /// window 닫기
        /// </summary>
        public void HidePanel()
        {
            if (_isTransitionRunning) return;
            if (!gameObject.activeSelf) return;

            if (_closePreset == null)
            {
                _uiWindow?.OnShow(false);
                gameObject.SetActive(false);
                return;
            }

            _isTransitionRunning = true;
            UIEffectService.Play(this, _effectTarget, _closePreset, () =>
            {
                _isTransitionRunning = false;
                _uiWindow?.OnShow(false);
                gameObject.SetActive(false);
            });
        }


        /// <summary>
        /// 실행 중인 전환과 콜백을 생략하고 즉시 표시 상태를 맞춥니다.
        /// </summary>
        public void SetVisibleImmediate(bool show, bool invokeOnShow)
        {
            if (_effectTarget != null)
            {
                UIEffectService.Stop(_effectTarget);
            }

            if (UiFadeUtility.TryGetCanvasGroup(gameObject, true, out var canvasGroup))
            {
                UiFadeUtility.StopFadeIfRunning(canvasGroup, this);
            }

            _isTransitionRunning = false;

            if (show)
            {
                gameObject.SetActive(true);
                UiFadeUtility.SetVisible(gameObject, true, true, true);

                if (invokeOnShow)
                {
                    _uiWindow?.OnShow(true);
                }

                return;
            }

            if (invokeOnShow)
            {
                _uiWindow?.OnShow(false);
            }

            UiFadeUtility.SetVisible(gameObject, false, true, true);
            gameObject.SetActive(false);
        }
    }
}
