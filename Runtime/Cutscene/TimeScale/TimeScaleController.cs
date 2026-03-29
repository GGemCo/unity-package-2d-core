using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 전역 Time.timeScale과 Time.fixedDeltaTime을 제어하는 컨트롤러입니다.
    /// Blend, 즉시 설정(Set), 복원(Restore) 모드를 지원하며,
    /// 컷신 종료 시 원래 상태로 복원하는 옵션을 제공합니다.
    /// </summary>
    public sealed class TimeScaleController : CutsceneDefaultController, ICutsceneController
    {
        /// <summary>
        /// 타임스케일 제어 상태를 나타냅니다.
        /// </summary>
        private enum PlaybackState
        {
            Idle,       // 아무 작업도 하지 않는 상태
            Blending,   // from → to로 보간 중
            Holding,    // 목표 값 유지 중
            Restoring,  // 원래 값으로 복원 중
            Completed   // 작업 완료
        }

        private TimeScaleData _data;
        private float _elapsed;
        private float _duration;
        private PlaybackState _state = PlaybackState.Idle;

        private float _restoreStartScale;
        private float _restoreStartFixedDeltaTime;
        private float _restoreTargetScale;
        private float _restoreTargetFixedDeltaTime;

        /// <summary>
        /// TimeScale 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">컷신 흐름을 관리하는 매니저입니다.</param>
        public TimeScaleController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 사전 준비 단계입니다. 현재는 별도 처리 없이 한 프레임을 양보합니다.
        /// </summary>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            yield return null;
        }

        /// <summary>
        /// TimeScale 이벤트를 시작하고 ActionMode에 따라 동작을 분기합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
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

        /// <summary>
        /// 현재 상태에 따라 보간 또는 복원 처리를 갱신합니다.
        /// </summary>
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

        /// <summary>
        /// 현재 진행 중인 작업을 즉시 종료하고 최종 상태를 적용합니다.
        /// </summary>
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

        /// <summary>
        /// 컷신 종료 시 설정에 따라 TimeScale을 복원합니다.
        /// 현재 컨트롤러가 TimeScale 소유자인 경우에만 복원을 수행합니다.
        /// </summary>
        public void End()
        {
            if (_data == null)
            {
                return;
            }

            if ((_state == PlaybackState.Blending ||
                 _state == PlaybackState.Holding ||
                 _state == PlaybackState.Restoring) &&
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

        /// <summary>
        /// 강제로 원래 TimeScale 상태로 복원합니다.
        /// </summary>
        public void ForceRestoreOriginalState()
        {
            if (TryResolveRestoreTarget(out var targetScale, out var targetFixedDeltaTime))
            {
                ApplyScale(targetScale, targetFixedDeltaTime);
            }

            FinalizeRestore();
        }

        /// <summary>
        /// Blend → Hold 모드를 시작합니다.
        /// </summary>
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

        /// <summary>
        /// 즉시 TimeScale을 설정하고 유지합니다.
        /// </summary>
        private void TriggerSetAndHold()
        {
            CutsceneManager.RegisterTimeScaleOwner(this);
            ApplyTimelineModeForCurrentData(_data.toScale);

            ApplyScale(_data.toScale);
            _state = PlaybackState.Holding;
        }

        /// <summary>
        /// 현재 TimeScale을 원래 값으로 복원하는 모드를 시작합니다.
        /// </summary>
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

        /// <summary>
        /// Blend 진행을 갱신하여 fromScale → toScale로 보간합니다.
        /// </summary>
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

        /// <summary>
        /// Restore 진행을 갱신하여 현재 값 → 목표 값으로 보간합니다.
        /// </summary>
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

        /// <summary>
        /// 복원 대상 TimeScale 값을 결정합니다.
        /// 캡처된 값이 있으면 이를 사용하고, 없으면 데이터 값을 사용합니다.
        /// </summary>
        private bool TryResolveRestoreTarget(out float targetScale, out float targetFixedDeltaTime)
        {
            if (_data is { useCapturedScaleForRestore: true } &&
                CutsceneManager.TryGetCapturedTimeScaleState(out targetScale, out targetFixedDeltaTime))
            {
                return true;
            }

            targetScale = Mathf.Max(0f, _data?.restoreScale ?? 1f);
            targetFixedDeltaTime = CalculateFixedDeltaTime(targetScale);
            return false;
        }

        /// <summary>
        /// 복원 목표 값을 즉시 적용합니다.
        /// </summary>
        private void ApplyRestoreTargetImmediate()
        {
            ApplyScale(_restoreTargetScale, _restoreTargetFixedDeltaTime);
        }

        /// <summary>
        /// 복원 완료 후 상태를 정리하고 TimeScale 소유권을 해제합니다.
        /// </summary>
        private void FinalizeRestore()
        {
            ApplyTimelineMode(false);
            _state = PlaybackState.Completed;
            CutsceneManager.ClearTimeScaleOwner(this);
        }

        /// <summary>
        /// 현재 설정에 따라 DeltaTime을 선택합니다.
        /// timeScale이 0일 경우에도 업데이트가 멈추지 않도록 처리합니다.
        /// </summary>
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

        /// <summary>
        /// Timeline이 timeScale 0에서도 동작할지 여부를 설정합니다.
        /// </summary>
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

        /// <summary>
        /// Timeline 업데이트 방식(timeScale vs unscaledTime)을 설정합니다.
        /// </summary>
        private void ApplyTimelineMode(bool useUnscaledTimelineTime)
        {
            CutsceneManager.SetTimeScaleTimelineMode(this, useUnscaledTimelineTime);
        }

        /// <summary>
        /// Time.timeScale과 필요 시 fixedDeltaTime을 적용합니다.
        /// </summary>
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

        /// <summary>
        /// Time.timeScale과 fixedDeltaTime을 동시에 적용합니다.
        /// </summary>
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

        /// <summary>
        /// 현재 scale에 맞는 fixedDeltaTime을 계산합니다.
        /// 캡처된 기준값이 있으면 이를 기반으로 계산합니다.
        /// </summary>
        private float CalculateFixedDeltaTime(float scale)
        {
            float baseFixedDeltaTime =
                CutsceneManager.TryGetCapturedTimeScaleState(out _, out float capturedFixedDeltaTime)
                    ? capturedFixedDeltaTime
                    : Time.fixedDeltaTime;

            float minimumScale = Mathf.Max(0f, _data?.minimumScaleForFixedDeltaTime ?? 0f);
            float safeScale = Mathf.Max(0f, scale);
            float fixedScale = minimumScale > 0f ? Mathf.Max(minimumScale, safeScale) : safeScale;

            return baseFixedDeltaTime * fixedScale;
        }
    }
}