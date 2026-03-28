using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 동안 전역 Time.timeScale과 fixedDeltaTime을 제어합니다.
    /// 적용 후 유지(Hold)와 별도 Restore 이벤트를 모두 지원합니다.
    /// </summary>
    public sealed class TimeScaleController : CutsceneDefaultController, ICutsceneController
    {
        private enum PlaybackState
        {
            Idle,
            Blending,
            Holding,
            Restoring,
            Completed
        }

        private TimeScaleData _data;
        private float _elapsed;
        private float _duration;
        private PlaybackState _state = PlaybackState.Idle;

        private float _restoreStartScale;
        private float _restoreStartFixedDeltaTime;
        private float _restoreTargetScale;
        private float _restoreTargetFixedDeltaTime;

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
            _duration = Mathf.Max(0f, evt.duration);
            _elapsed = 0f;

            switch (_data.actionMode)
            {
                case TimeScaleActionMode.BlendAndHold:
                    TriggerBlendAndHold();
                    break;

                case TimeScaleActionMode.SetAndHold:
                    TriggerSetAndHold();
                    break;

                case TimeScaleActionMode.Restore:
                    TriggerRestore();
                    break;
            }
        }

        public void Update()
        {
            if (_data == null)
            {
                return;
            }

            switch (_state)
            {
                case PlaybackState.Blending:
                    UpdateBlendAndHold();
                    break;
                case PlaybackState.Restoring:
                    UpdateRestore();
                    break;
            }
        }

        public void Stop()
        {
            switch (_state)
            {
                case PlaybackState.Blending:
                    ApplyScale(_data.toScale);
                    _state = PlaybackState.Holding;
                    break;

                case PlaybackState.Restoring:
                    ApplyRestoreTargetImmediate();
                    FinalizeRestore();
                    break;
            }
        }

        public void End()
        {
            if (_data == null)
            {
                return;
            }

            if ((_state == PlaybackState.Blending || _state == PlaybackState.Holding || _state == PlaybackState.Restoring) &&
                _data.restoreOnCutsceneEnd &&
                CutsceneManager.IsActiveTimeScaleOwner(this))
            {
                if (TryResolveRestoreTarget(out var targetScale, out var targetFixedDeltaTime))
                {
                    ApplyScale(targetScale, targetFixedDeltaTime);
                }

                FinalizeRestore();
                return;
            }

            if (_state == PlaybackState.Restoring)
            {
                ApplyRestoreTargetImmediate();
                FinalizeRestore();
            }
        }

        public void ForceRestoreOriginalState()
        {
            if (TryResolveRestoreTarget(out var targetScale, out var targetFixedDeltaTime))
            {
                ApplyScale(targetScale, targetFixedDeltaTime);
            }

            FinalizeRestore();
        }

        private void TriggerBlendAndHold()
        {
            CutsceneManager.RegisterTimeScaleOwner(this);
            ApplyTimelineModeForCurrentData(_data.toScale);
            _state = _duration > 0f ? PlaybackState.Blending : PlaybackState.Holding;

            if (_duration <= 0f)
            {
                ApplyScale(_data.toScale);
                return;
            }

            ApplyScale(_data.fromScale);
        }

        private void TriggerSetAndHold()
        {
            CutsceneManager.RegisterTimeScaleOwner(this);
            ApplyTimelineModeForCurrentData(_data.toScale);
            ApplyScale(_data.toScale);
            _state = PlaybackState.Holding;
        }

        private void TriggerRestore()
        {
            ApplyTimelineMode(false);
            _restoreStartScale = Time.timeScale;
            _restoreStartFixedDeltaTime = Time.fixedDeltaTime;

            if (!TryResolveRestoreTarget(out _restoreTargetScale, out _restoreTargetFixedDeltaTime))
            {
                _restoreTargetScale = Mathf.Max(0f, _data.restoreScale);
                _restoreTargetFixedDeltaTime = CalculateFixedDeltaTime(_restoreTargetScale);
            }

            if (_duration <= 0f)
            {
                ApplyRestoreTargetImmediate();
                FinalizeRestore();
                return;
            }

            _state = PlaybackState.Restoring;
        }

        private void UpdateBlendAndHold()
        {
            _elapsed += GetControllerDeltaTime();
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(t, _data.easing));
            float scale = Mathf.Lerp(_data.fromScale, _data.toScale, eased);
            ApplyScale(scale);

            if (_elapsed >= _duration)
            {
                Stop();
            }
        }

        private void UpdateRestore()
        {
            _elapsed += GetControllerDeltaTime();
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(t, _data.easing));
            float scale = Mathf.Lerp(_restoreStartScale, _restoreTargetScale, eased);

            if (_data.affectFixedDeltaTime)
            {
                float fixedDelta = Mathf.Lerp(_restoreStartFixedDeltaTime, _restoreTargetFixedDeltaTime, eased);
                ApplyScale(scale, fixedDelta);
            }
            else
            {
                ApplyScale(scale);
            }

            if (_elapsed >= _duration)
            {
                Stop();
            }
        }

        private bool TryResolveRestoreTarget(out float targetScale, out float targetFixedDeltaTime)
        {
            if (_data != null && _data.useCapturedScaleForRestore &&
                CutsceneManager.TryGetCapturedTimeScaleState(out targetScale, out targetFixedDeltaTime))
            {
                return true;
            }

            targetScale = Mathf.Max(0f, _data != null ? _data.restoreScale : 1f);
            targetFixedDeltaTime = CalculateFixedDeltaTime(targetScale);
            return false;
        }

        private void ApplyRestoreTargetImmediate()
        {
            ApplyScale(_restoreTargetScale, _restoreTargetFixedDeltaTime);
        }

        private void FinalizeRestore()
        {
            ApplyTimelineMode(false);
            _state = PlaybackState.Completed;
            CutsceneManager.ClearTimeScaleOwner(this);
        }

        private float GetControllerDeltaTime()
        {
            if (_data == null)
            {
                return Time.deltaTime;
            }

            if (_data.useUnscaledTime)
            {
                return Time.unscaledDeltaTime;
            }

            if (Mathf.Approximately(Time.timeScale, 0f))
            {
                return Time.unscaledDeltaTime;
            }

            return Time.deltaTime;
        }

        private void ApplyTimelineModeForCurrentData(float targetScale)
        {
            if (_data == null)
            {
                ApplyTimelineMode(false);
                return;
            }

            bool shouldKeepRunning = _data.timelineMode switch
            {
                CutsceneTimeScaleTimelineMode.KeepRunningWhenTimeScaleIsZero => Mathf.Approximately(targetScale, 0f),
                CutsceneTimeScaleTimelineMode.PauseWithTimeScale => false,
                _ => false,
            };

            ApplyTimelineMode(shouldKeepRunning);
        }

        private void ApplyTimelineMode(bool useUnscaledTimelineTime)
        {
            CutsceneManager.SetTimeScaleTimelineMode(this, useUnscaledTimelineTime);
        }

        private void ApplyScale(float scale)
        {
            float safeScale = Mathf.Max(0f, scale);
            Time.timeScale = safeScale;

            if (_data == null || !_data.affectFixedDeltaTime)
            {
                return;
            }

            Time.fixedDeltaTime = CalculateFixedDeltaTime(safeScale);
        }

        private void ApplyScale(float scale, float fixedDeltaTime)
        {
            float safeScale = Mathf.Max(0f, scale);
            Time.timeScale = safeScale;

            if (_data == null || !_data.affectFixedDeltaTime)
            {
                return;
            }

            Time.fixedDeltaTime = Mathf.Max(0f, fixedDeltaTime);
        }

        private float CalculateFixedDeltaTime(float scale)
        {
            float baseFixedDeltaTime = CutsceneManager.TryGetCapturedTimeScaleState(out _, out float capturedFixedDeltaTime)
                ? capturedFixedDeltaTime
                : Time.fixedDeltaTime;

            float minimumScale = Mathf.Max(0f, _data?.minimumScaleForFixedDeltaTime ?? 0f);
            float safeScale = Mathf.Max(0f, scale);
            float fixedScale = minimumScale > 0f ? Mathf.Max(minimumScale, safeScale) : safeScale;
            return baseFixedDeltaTime * fixedScale;
        }
    }
}
