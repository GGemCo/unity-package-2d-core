using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 UI Panel의 위치, 크기, 색상, 알파를 제어하는 컨트롤러입니다.
    /// Panel 생성, 상태 보간(Interpolation), 종료 시 제거/숨김까지 관리합니다.
    /// </summary>
    public sealed class UiPanelController : CutsceneDefaultController, ICutsceneController
    {
        private CutsceneUiPanelPresenter _presenter;
        private UiPanelData _data;
        private float _elapsed;
        private float _duration;
        private bool _isPlaying;

        /// <summary>
        /// UI Panel 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">컷신 흐름을 관리하는 매니저입니다.</param>
        public UiPanelController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// UI Panel 이벤트 실행 전 Presenter를 준비하고,
        /// 필요 시 Panel을 미리 생성합니다.
        /// </summary>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.UiPanel)
            {
                yield break;
            }

            _presenter = CutsceneManager.GetOrCreateUiPanelPresenter();

            var data = evt.uiPanel ?? new UiPanelData();

            if (_presenter != null)
            {
                _presenter.ApplyRenderSettings(data, SceneGame.Instance);

                // 미리 Panel 생성 (옵션)
                if (data.createIfMissing)
                {
                    _presenter.ConfigurePanel(data.panelId, data);
                }
            }

            yield return null;
        }

        /// <summary>
        /// UI Panel을 생성 또는 설정하고 애니메이션을 시작합니다.
        /// duration이 0 이하이면 즉시 최종 상태를 적용합니다.
        /// </summary>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.UiPanel)
            {
                return;
            }

            _presenter ??= CutsceneManager.GetOrCreateUiPanelPresenter();
            if (_presenter == null)
            {
                return;
            }

            _data = evt.uiPanel ?? new UiPanelData();

            _presenter.ApplyRenderSettings(_data, SceneGame.Instance);

            // Panel이 없고 생성 옵션도 없으면 종료
            if (!_data.createIfMissing && !_presenter.HasPanel(_data.panelId))
            {
                return;
            }

            // Panel 설정 (생성 또는 갱신)
            _presenter.ConfigurePanel(_data.panelId, _data);

            _duration = evt.duration > 0f ? evt.duration : 0f;
            _elapsed = 0f;
            _isPlaying = _duration > 0f;

            if (_duration <= 0f)
            {
                ApplyFinalState();
                return;
            }

            ApplyStartState();
        }

        /// <summary>
        /// 시간 경과에 따라 Panel 상태를 보간하여 적용합니다.
        /// </summary>
        public void Update()
        {
            if (!_isPlaying || _presenter == null || _data == null)
            {
                return;
            }

            _elapsed += _data.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float normalized = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(normalized, _data.easing));

            ApplyInterpolatedState(eased);

            if (_elapsed >= _duration)
            {
                Stop();
            }
        }

        /// <summary>
        /// Panel 애니메이션을 종료하고 최종 상태를 적용합니다.
        /// 옵션에 따라 Panel을 제거하거나 숨깁니다.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;

            if (_presenter == null || _data == null)
            {
                return;
            }

            ApplyFinalState();

            if (_data.destroyOnStop)
            {
                _presenter.DestroyPanel(_data.panelId);
                return;
            }

            if (_data.hideOnStop)
            {
                _presenter.SetPanelVisible(_data.panelId, false);
            }
        }

        /// <summary>
        /// 컷신 종료 시 Panel 상태를 정리합니다.
        /// Stop과 동일하게 제거 또는 숨김 정책을 따릅니다.
        /// </summary>
        public void End()
        {
            _isPlaying = false;

            if (_presenter == null || _data == null)
            {
                return;
            }

            if (_data.destroyOnStop)
            {
                _presenter.DestroyPanel(_data.panelId);
                return;
            }

            if (_data.hideOnStop)
            {
                _presenter.SetPanelVisible(_data.panelId, false);
            }
        }

        /// <summary>
        /// 애니메이션 시작 상태를 적용합니다.
        /// </summary>
        private void ApplyStartState()
        {
            _presenter.ApplyRenderSettings(_data, SceneGame.Instance);
            _presenter.ConfigurePanel(_data.panelId, _data);

            _presenter.ApplyState(
                _data.panelId,
                _data.fromAnchoredPosition,
                _data.fromSizeDelta,
                _data.fromColor,
                _data.fromAlpha);

            _presenter.SetPanelVisible(_data.panelId, true);
        }

        /// <summary>
        /// 애니메이션 종료 상태를 적용합니다.
        /// </summary>
        private void ApplyFinalState()
        {
            _presenter.ApplyRenderSettings(_data, SceneGame.Instance);
            _presenter.ConfigurePanel(_data.panelId, _data);

            _presenter.ApplyState(
                _data.panelId,
                _data.toAnchoredPosition,
                _data.toSizeDelta,
                _data.toColor,
                _data.toAlpha);

            _presenter.SetPanelVisible(_data.panelId, true);
        }

        /// <summary>
        /// 보간 값에 따라 Panel의 위치, 크기, 색상, 알파를 계산하여 적용합니다.
        /// </summary>
        /// <param name="t">0~1 범위의 보간 값입니다.</param>
        private void ApplyInterpolatedState(float t)
        {
            var position = new Vec2(Vector2.Lerp(
                _data.fromAnchoredPosition.ToVector2(),
                _data.toAnchoredPosition.ToVector2(),
                t));

            var sizeDelta = new Vec2(Vector2.Lerp(
                _data.fromSizeDelta.ToVector2(),
                _data.toSizeDelta.ToVector2(),
                t));

            var color = Color.Lerp(_data.fromColor, _data.toColor, t);
            float alpha = Mathf.Lerp(_data.fromAlpha, _data.toAlpha, t);

            _presenter.ApplyState(_data.panelId, position, sizeDelta, color, alpha);
            _presenter.SetPanelVisible(_data.panelId, true);
        }
    }
}