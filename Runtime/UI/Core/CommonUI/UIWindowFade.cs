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
        private bool _transitionTargetVisible;
        private int _transitionVersion;

        private void Awake()
        {
            if (_uiWindow == null)
                _uiWindow = GetComponent<UIWindowBase>();

            if (_uiWindow != null && !_uiWindow.useFade)
            {
                enabled = false;
                return;
            }

            if (_effectTarget == null)
                _effectTarget = UIEffectTarget.GetOrAdd(gameObject);
        }

        /// <summary>
        /// 윈도우 Fade 전환에 필요한 참조와 프리셋을 초기화합니다.
        /// </summary>
        /// <param name="windowBase">Fade 전환을 소유한 윈도우입니다.</param>
        /// <param name="effectTarget">UI Effect 실행 대상입니다.</param>
        /// <param name="openPreset">윈도우 열기 효과 프리셋입니다.</param>
        /// <param name="closePreset">윈도우 닫기 효과 프리셋입니다.</param>
        public void Initialize(UIWindowBase windowBase, UIEffectTarget effectTarget, UIEffectPreset openPreset, UIEffectPreset closePreset)
        {
            _uiWindow = windowBase;
            _effectTarget = effectTarget != null ? effectTarget : UIEffectTarget.GetOrAdd(gameObject);
            _openPreset = openPreset;
            _closePreset = closePreset;
        }

        /// <summary>
        /// 윈도우 열기 전환을 시작합니다.
        /// 닫기 전환 중 호출되면 기존 효과를 취소하고 최신 열기 요청을 적용합니다.
        /// </summary>
        public void ShowPanel()
        {
            bool wasActive = gameObject.activeSelf;
            if (_isTransitionRunning)
            {
                if (_transitionTargetVisible)
                {
                    return;
                }

                CancelRunningTransition();
            }
            else if (wasActive)
            {
                return;
            }

            if (!wasActive)
            {
                gameObject.SetActive(true);
                _uiWindow?.OnShow(true);
            }

            if (_openPreset == null)
            {
                UiFadeUtility.SetVisible(gameObject, true, true, true);
                return;
            }

            StartTransition(_openPreset, targetVisible: true);
        }

        /// <summary>
        /// 윈도우 닫기 전환을 시작합니다.
        /// 열기 전환 중 호출되면 기존 효과를 취소하고 최신 닫기 요청을 적용합니다.
        /// </summary>
        public void HidePanel()
        {
            if (_isTransitionRunning)
            {
                if (!_transitionTargetVisible)
                {
                    return;
                }

                CancelRunningTransition();
            }

            if (!gameObject.activeSelf)
            {
                return;
            }

            if (_closePreset == null)
            {
                _uiWindow?.OnShow(false);
                UiFadeUtility.SetVisible(gameObject, false, true, true);
                gameObject.SetActive(false);
                return;
            }

            StartTransition(_closePreset, targetVisible: false);
        }

        /// <summary>
        /// 지정한 표시 상태를 목표로 UI 효과 전환을 시작합니다.
        /// 버전 값을 사용하여 취소된 이전 효과의 완료 콜백이 최신 상태를 덮어쓰지 못하게 합니다.
        /// </summary>
        /// <param name="preset">실행할 UI 효과 프리셋입니다.</param>
        /// <param name="targetVisible">전환 완료 후 윈도우를 표시할지 여부입니다.</param>
        private void StartTransition(UIEffectPreset preset, bool targetVisible)
        {
            _isTransitionRunning = true;
            _transitionTargetVisible = targetVisible;
            int transitionVersion = ++_transitionVersion;

            UIEffectService.Play(this, _effectTarget, preset, () =>
            {
                if (!_isTransitionRunning || transitionVersion != _transitionVersion)
                {
                    return;
                }

                _isTransitionRunning = false;
                if (targetVisible)
                {
                    UiFadeUtility.SetVisible(gameObject, true, true, true);
                    return;
                }

                _uiWindow?.OnShow(false);
                UiFadeUtility.SetVisible(gameObject, false, true, true);
                gameObject.SetActive(false);
            });
        }

        /// <summary>
        /// 진행 중인 UI 효과와 보조 Fade를 중단하고 이전 완료 콜백을 무효화합니다.
        /// 현재 시각 상태는 유지하여 반대 방향 전환이 그 지점에서 자연스럽게 이어지도록 합니다.
        /// </summary>
        private void CancelRunningTransition()
        {
            _transitionVersion++;

            if (_effectTarget != null)
            {
                UIEffectService.Stop(_effectTarget);
            }

            if (UiFadeUtility.TryGetCanvasGroup(gameObject, true, out CanvasGroup canvasGroup))
            {
                UiFadeUtility.StopFadeIfRunning(canvasGroup, this);
            }

            _isTransitionRunning = false;
        }

        /// <summary>
        /// 실행 중인 전환과 콜백을 생략하고 즉시 표시 상태를 맞춥니다.
        /// </summary>
        /// <param name="show">윈도우를 즉시 표시하면 <see langword="true"/>, 숨기면 <see langword="false"/>입니다.</param>
        /// <param name="invokeOnShow">표시 상태 변경 생명주기 콜백을 호출할지 여부입니다.</param>
        public void SetVisibleImmediate(bool show, bool invokeOnShow)
        {
            CancelRunningTransition();

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
