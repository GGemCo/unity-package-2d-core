using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 화면 전체 페이드를 제어하는 컷신 컨트롤러입니다.
    /// </summary>
    public sealed class ScreenFadeController : CutsceneDefaultController, ICutsceneController
    {
        private ScreenFadePresenter _presenter;
        private ScreenFadeData _data;
        private float _elapsed;
        private float _duration;
        private bool _isPlaying;

        public ScreenFadeController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.ScreenFade)
            {
                yield break;
            }

            _presenter = CutsceneManager.GetOrCreateScreenFadePresenter(evt.screenFade);
            yield return null;
        }

        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.ScreenFade)
            {
                return;
            }

            _presenter = CutsceneManager.GetOrCreateScreenFadePresenter(evt.screenFade);
            if (_presenter == null)
            {
                return;
            }

            _data = evt.screenFade ?? new ScreenFadeData();
            _presenter?.ApplyRenderSettings(_data, SceneGame.Instance);
            _duration = evt.duration > 0f ? evt.duration : 0f;
            _elapsed = 0f;
            _isPlaying = _duration > 0f;

            if (_duration <= 0f)
            {
                _presenter.SetFade(_data.color, _data.toAlpha, _data.toAlpha > 0f);
                return;
            }

            _presenter.SetFade(_data.color, _data.fromAlpha, true);
        }

        public void Update()
        {
            if (!_isPlaying || _presenter == null || _data == null)
            {
                return;
            }

            _elapsed += _data.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(t, _data.easing));
            float alpha = Mathf.Lerp(_data.fromAlpha, _data.toAlpha, eased);
            _presenter.SetFade(_data.color, alpha, true);

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

            if (_data.holdFinalState)
            {
                _presenter.SetFade(_data.color, _data.toAlpha, _data.toAlpha > 0f);
            }
            else
            {
                _presenter.SetFade(_data.color, 0f, false);
            }
        }

        public void End()
        {
            _isPlaying = false;
        }
    }
}
