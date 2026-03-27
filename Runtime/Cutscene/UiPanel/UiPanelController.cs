using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 UI Panel의 위치, 크기, 색상, 알파를 제어합니다.
    /// </summary>
    public sealed class UiPanelController : CutsceneDefaultController, ICutsceneController
    {
        private CutsceneUiPanelPresenter _presenter;
        private UiPanelData _data;
        private float _elapsed;
        private float _duration;
        private bool _isPlaying;

        public UiPanelController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.UiPanel)
            {
                yield break;
            }

            _presenter = CutsceneManager.GetOrCreateUiPanelPresenter();
            var data = evt.uiPanel ?? new UiPanelData();
            if (_presenter != null && data.createIfMissing)
            {
                _presenter.ConfigurePanel(data.panelId, data);
            }

            yield return null;
        }

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
            if (!_data.createIfMissing && !_presenter.HasPanel(_data.panelId))
            {
                return;
            }

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

        private void ApplyStartState()
        {
            _presenter.ConfigurePanel(_data.panelId, _data);
            _presenter.ApplyState(_data.panelId, _data.fromAnchoredPosition, _data.fromSizeDelta, _data.fromColor, _data.fromAlpha);
            _presenter.SetPanelVisible(_data.panelId, true);
        }

        private void ApplyFinalState()
        {
            _presenter.ConfigurePanel(_data.panelId, _data);
            _presenter.ApplyState(_data.panelId, _data.toAnchoredPosition, _data.toSizeDelta, _data.toColor, _data.toAlpha);
            _presenter.SetPanelVisible(_data.panelId, true);
        }

        private void ApplyInterpolatedState(float t)
        {
            var position = new Vec2(Vector2.Lerp(_data.fromAnchoredPosition.ToVector2(), _data.toAnchoredPosition.ToVector2(), t));
            var sizeDelta = new Vec2(Vector2.Lerp(_data.fromSizeDelta.ToVector2(), _data.toSizeDelta.ToVector2(), t));
            var color = Color.Lerp(_data.fromColor, _data.toColor, t);
            float alpha = Mathf.Lerp(_data.fromAlpha, _data.toAlpha, t);

            _presenter.ApplyState(_data.panelId, position, sizeDelta, color, alpha);
            _presenter.SetPanelVisible(_data.panelId, true);
        }
    }
}
