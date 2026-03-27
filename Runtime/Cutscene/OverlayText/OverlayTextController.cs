using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 화면 중앙 텍스트를 출력하는 컨트롤러입니다.
    /// </summary>
    public sealed class OverlayTextController : CutsceneDefaultController, ICutsceneController
    {
        private CutsceneOverlayPresenter _presenter;
        private OverlayTextData _data;
        private float _elapsed;
        private float _duration;
        private bool _isPlaying;

        public OverlayTextController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.OverlayText)
            {
                yield break;
            }

            _presenter = CutsceneManager.GetOrCreateOverlayPresenter();
            yield return null;
        }

        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.OverlayText)
            {
                return;
            }

            _presenter ??= CutsceneManager.GetOrCreateOverlayPresenter();
            if (_presenter == null)
            {
                return;
            }

            _data = evt.overlayText ?? new OverlayTextData();
            _duration = evt.duration > 0f ? evt.duration : 0f;
            _elapsed = 0f;
            _isPlaying = _duration > 0f;

            _presenter.ConfigureOverlayText(_data);
            _presenter.SetOverlayTextVisible(true);

            if (_duration <= 0f)
            {
                _presenter.SetOverlayTextAlpha(_data.maxAlpha);
                return;
            }

            _presenter.SetOverlayTextAlpha(_data.fadeIn ? 0f : _data.maxAlpha);
        }

        public void Update()
        {
            if (!_isPlaying || _presenter == null || _data == null)
            {
                return;
            }

            _elapsed += _data.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float alpha = EvaluateAlpha();
            _presenter.SetOverlayTextAlpha(alpha);

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

            if (_data.fadeOut)
            {
                _presenter.SetOverlayTextAlpha(0f);
                _presenter.SetOverlayTextVisible(false);
            }
            else
            {
                _presenter.SetOverlayTextAlpha(_data.maxAlpha);
            }
        }

        public void End()
        {
            _isPlaying = false;
            if (_presenter != null)
            {
                _presenter.SetOverlayTextVisible(false);
            }
        }

        private float EvaluateAlpha()
        {
            if (_duration <= 0f)
            {
                return _data.maxAlpha;
            }

            float normalized = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(normalized, _data.easing));

            if (_data.fadeIn && _data.fadeOut)
            {
                if (normalized < 0.2f)
                {
                    float t = Mathf.Clamp01(normalized / 0.2f);
                    return Mathf.Lerp(0f, _data.maxAlpha, Mathf.Clamp01(Easing.Apply(t, _data.easing)));
                }

                if (normalized > 0.8f)
                {
                    float t = Mathf.Clamp01((normalized - 0.8f) / 0.2f);
                    return Mathf.Lerp(_data.maxAlpha, 0f, Mathf.Clamp01(Easing.Apply(t, _data.easing)));
                }

                return _data.maxAlpha;
            }

            if (_data.fadeIn)
            {
                return Mathf.Lerp(0f, _data.maxAlpha, eased);
            }

            if (_data.fadeOut)
            {
                return Mathf.Lerp(_data.maxAlpha, 0f, eased);
            }

            return _data.maxAlpha;
        }
    }
}
