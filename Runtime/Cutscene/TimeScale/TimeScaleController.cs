using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 동안 전역 Time.timeScale과 fixedDeltaTime을 제어합니다.
    /// </summary>
    public sealed class TimeScaleController : CutsceneDefaultController, ICutsceneController
    {
        private TimeScaleData _data;
        private float _elapsed;
        private float _duration;
        private bool _isPlaying;
        private bool _hasCapturedOriginalState;
        private float _originalTimeScale;
        private float _originalFixedDeltaTime;

        public TimeScaleController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            yield return null;
        }

        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.TimeScale)
            {
                return;
            }

            _data = evt.timeScale ?? new TimeScaleData();
            CaptureOriginalStateIfNeeded();

            _duration = Mathf.Max(0f, evt.duration);
            _elapsed = 0f;
            _isPlaying = _duration > 0f;

            if (_duration <= 0f)
            {
                ApplyScale(_data.toScale);
                return;
            }

            ApplyScale(_data.fromScale);
        }

        public void Update()
        {
            if (!_isPlaying || _data == null)
            {
                return;
            }

            _elapsed += _data.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(t, _data.easing));
            float scale = Mathf.Lerp(_data.fromScale, _data.toScale, eased);
            ApplyScale(scale);

            if (_elapsed >= _duration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            _isPlaying = false;
            if (_data == null)
            {
                return;
            }

            if (_data.restoreOnStop)
            {
                RestoreOriginalState();
            }
            else
            {
                ApplyScale(_data.toScale);
            }
        }

        public void End()
        {
            _isPlaying = false;
            if (_data != null && _data.restoreOnCutsceneEnd)
            {
                RestoreOriginalState();
            }
        }

        public void ForceRestoreOriginalState()
        {
            _isPlaying = false;
            RestoreOriginalState();
        }

        private void CaptureOriginalStateIfNeeded()
        {
            if (_hasCapturedOriginalState)
            {
                return;
            }

            _originalTimeScale = Time.timeScale;
            _originalFixedDeltaTime = Time.fixedDeltaTime;
            _hasCapturedOriginalState = true;
        }

        private void ApplyScale(float scale)
        {
            float safeScale = Mathf.Max(0f, scale);
            Time.timeScale = safeScale;

            if (_data == null || !_data.affectFixedDeltaTime)
            {
                return;
            }

            float minimumScale = Mathf.Max(0f, _data.minimumScaleForFixedDeltaTime);
            float fixedScale = safeScale;
            if (minimumScale > 0f)
            {
                fixedScale = Mathf.Max(minimumScale, safeScale);
            }

            Time.fixedDeltaTime = _originalFixedDeltaTime * fixedScale;
        }

        private void RestoreOriginalState()
        {
            if (!_hasCapturedOriginalState)
            {
                return;
            }

            Time.timeScale = _originalTimeScale;
            if (_data == null || _data.affectFixedDeltaTime)
            {
                Time.fixedDeltaTime = _originalFixedDeltaTime;
            }

            _hasCapturedOriginalState = false;
        }
    }
}
