#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// SetupStep 파이프라인을 "에디터 틱(EditorApplication.update)" 기반으로 실행하는 러너입니다.
    /// </summary>
    /// <remarks>
    /// - 스텝 단위로 Validate/Execute를 수행하여, 진행 중에도 EditorWindow UI가 갱신될 수 있게 합니다.
    /// - 개별 스텝은 동기(블로킹)로 실행되므로, "긴 단일 스텝"은 별도 분해가 필요할 수 있습니다.
    /// - Progress API와 연동하여 에디터 Progress 창에도 진행 상태를 보고합니다.
    /// </remarks>
    public sealed class SetupRunner : IDisposable
    {
        /// <summary>
        /// 러너의 전체 실행 상태입니다.
        /// </summary>
        public enum RunState
        {
            /// <summary>대기 상태입니다.</summary>
            Idle,

            /// <summary>실행 중입니다.</summary>
            Running,

            /// <summary>모든 단계가 성공적으로 완료되었습니다.</summary>
            Succeeded,

            /// <summary>일부 단계 실패를 포함하여 완료되었습니다.</summary>
            Failed,

            /// <summary>취소 요청으로 중단되었습니다.</summary>
            Canceled
        }

        /// <summary>
        /// 스텝이 수행되는 단계(Phase)입니다.
        /// </summary>
        public enum StepPhase
        {
            /// <summary>실행 전 유효성 검사 단계입니다.</summary>
            Validate,

            /// <summary>실제 적용(설치/복사/등록 등) 단계입니다.</summary>
            Execute
        }

        /// <summary>
        /// 스텝 단위의 결과 상태입니다.
        /// </summary>
        public enum StepResult
        {
            /// <summary>현재 스텝이 실행 중입니다.</summary>
            Running,

            /// <summary>스텝이 성공했습니다.</summary>
            Succeeded,

            /// <summary>스텝이 실패했습니다.</summary>
            Failed,

            /// <summary>스텝이 수행되지 않고 건너뛰어졌습니다.</summary>
            Skipped
        }

        /// <summary>
        /// 스텝 상태 변경 이벤트 핸들러 시그니처입니다.
        /// </summary>
        /// <param name="stepIndex">대상 스텝의 인덱스(0 기반)입니다.</param>
        /// <param name="phase">현재 단계(Validate/Execute)입니다.</param>
        /// <param name="result">스텝의 결과 상태입니다.</param>
        /// <param name="message">보조 메시지(스킵 사유/검증 실패 사유/예외 메시지 등)입니다.</param>
        public delegate void StepStateChanged(int stepIndex, StepPhase phase, StepResult result, string message);

        /// <summary>
        /// 스텝 상태 변경 이벤트입니다.
        /// </summary>
        public event StepStateChanged OnStepStateChanged;

        /// <summary>
        /// 실시간 로그 라인 이벤트입니다.
        /// </summary>
        public event Action<string> OnLogLine;

        /// <summary>
        /// 전체 완료(성공/실패/취소) 이벤트입니다.
        /// </summary>
        public event Action<SetupRunner> OnCompleted;

        private readonly SetupStepBase[] _steps;
        private readonly bool _validateOnly;

        private EditorSetupLogger _logger;
        private EditorSetupContext _ctx;

        private int _progressId = -1;
        private int _index = -1;

        private bool _cancelRequested;
        private bool _hasAnyFailure;

        private StepPhase _phase = StepPhase.Validate;

        /// <summary>
        /// 현재 러너의 실행 상태입니다.
        /// </summary>
        public RunState State { get; private set; } = RunState.Idle;

        /// <summary>
        /// 러너가 현재 실행 중인지 여부입니다.
        /// </summary>
        public bool IsRunning => State == RunState.Running;

        /// <summary>
        /// 러너가 기록 중인 로그 파일 경로입니다. (로거가 없으면 빈 문자열)
        /// </summary>
        public string LogPath => _logger != null ? _logger.LogPath : string.Empty;

        /// <summary>
        /// UI 표시용 설명 문자열(현재 단계/스텝 등)입니다.
        /// </summary>
        public string Description { get; private set; } = "대기 중...";

        /// <summary>
        /// 전체 진행률(0~1)입니다. ValidateOnly면 1-phase, 일반 모드면 Validate+Execute 2-phase로 환산됩니다.
        /// </summary>
        public float OverallProgress01 { get; private set; }

        /// <summary>
        /// 현재 단계(Validate/Execute)의 표시 문자열입니다.
        /// </summary>
        public string PhaseDisplay => _phase == StepPhase.Validate ? "Validate" : "Execute";

        /// <summary>
        /// 현재 인덱스 기준의 스텝 표시 이름입니다.
        /// </summary>
        /// <remarks>
        /// 내부 인덱스가 -1이거나 범위를 벗어날 수 있으므로 Clamp 처리합니다.
        /// </remarks>
        public string CurrentStepDisplay
        {
            get
            {
                if (_steps == null || _steps.Length == 0) return "(none)";
                int idx = Mathf.Clamp(_index, 0, _steps.Length - 1);
                var step = _steps[idx];
                return step != null ? step.DisplayName : "(null)";
            }
        }

        /// <summary>
        /// 러너를 생성합니다.
        /// </summary>
        /// <param name="steps">실행할 스텝 배열입니다. null이면 빈 배열로 처리됩니다.</param>
        /// <param name="validateOnly">true면 Validate만 수행하고 Execute는 수행하지 않습니다.</param>
        public SetupRunner(SetupStepBase[] steps, bool validateOnly)
        {
            _steps = steps ?? Array.Empty<SetupStepBase>();
            _validateOnly = validateOnly;
        }

        /// <summary>
        /// 러너 실행을 시작합니다.
        /// </summary>
        /// <remarks>
        /// EditorApplication.update에 Tick을 등록하여 "에디터 틱" 기반으로 단계적으로 실행됩니다.
        /// </remarks>
        /// <exception cref="Exception">
        /// Progress API 호출/로거 초기화 등에서 예외가 발생할 수 있습니다.
        /// (일반적으로는 에디터 환경 이슈나 IO 문제에 의해 발생 가능)
        /// </exception>
        public void Start()
        {
            if (IsRunning) return;

            if (_steps.Length == 0)
            {
                State = RunState.Failed;
                OnCompleted?.Invoke(this);
                return;
            }

            _logger = new EditorSetupLogger();
            _logger.OnLineAppended += HandleLogLine;
            var addressableEditor = ScriptableObject.CreateInstance<AddressableEditor>();
            _ctx = new EditorSetupContext(_logger, addressableEditor);

            _progressId = Progress.Start("GGemCo Project Setup", "Initializing...");
            Description = _validateOnly ? "Validate Only 모드로 실행합니다." : "Validate 후 Execute를 수행합니다.";
            OverallProgress01 = 0f;

            _cancelRequested = false;
            _hasAnyFailure = false;

            _phase = StepPhase.Validate;
            _index = -1;

            State = RunState.Running;
            EditorApplication.update += Tick;
        }

        /// <summary>
        /// 취소를 요청합니다.
        /// </summary>
        /// <remarks>
        /// 취소는 "현재 스텝 종료 후" 스텝 단위로 중단됩니다.
        /// </remarks>
        public void RequestCancel()
        {
            if (!IsRunning) return;
            _cancelRequested = true;
            Description = "취소 요청됨... (현재 스텝 종료 후 중단됩니다)";
        }

        /// <summary>
        /// 에디터 업데이트 틱마다 호출되어, 스텝을 한 번씩 진행합니다.
        /// </summary>
        /// <remarks>
        /// 스텝은 동기 실행이므로 Tick 내에서 블로킹될 수 있습니다.
        /// </remarks>
        private void Tick()
        {
            if (!IsRunning)
                return;

            // 취소는 "스텝 단위" 중단
            if (_cancelRequested)
            {
                Finish(RunState.Canceled);
                return;
            }

            // 다음 스텝 인덱스 계산
            _index++;

            // Phase 전환/완료 처리
            if (_index >= _steps.Length)
            {
                if (_phase == StepPhase.Validate && !_validateOnly)
                {
                    _phase = StepPhase.Execute;
                    _index = 0; // execute는 0부터
                }
                else
                {
                    // Execute까지 끝났거나 ValidateOnly 끝남
                    if (!_validateOnly)
                    {
                        try
                        {
                            EditorSceneManager.SaveOpenScenes();
                            _logger.Info("[Save] Open Scenes saved.");
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"[Save] 실패 :: {ex}");
                            _hasAnyFailure = true;
                        }
                    }

                    Finish(_hasAnyFailure ? RunState.Failed : RunState.Succeeded);
                    return;
                }
            }

            // 현재 스텝 실행
            var step = _steps[_index];
            if (step == null)
            {
                RaiseStepState(_index, _phase, StepResult.Skipped, "null step");
                ReportProgress();
                return;
            }

            if (!step.enabledStep)
            {
                RaiseStepState(_index, _phase, StepResult.Skipped, "disabled");
                ReportProgress();
                return;
            }

            RaiseStepState(_index, _phase, StepResult.Running, null);

            try
            {
                if (_phase == StepPhase.Validate)
                {
                    if (!step.Validate(_ctx, out var msg))
                    {
                        // 기존 정책: Validate 실패는 경고로 남기고 계속 진행
                        _logger.Warn($"[Validate] {step.DisplayName} :: {msg}");
                        RaiseStepState(_index, _phase, StepResult.Failed, msg);
                        _hasAnyFailure = true;
                    }
                    else
                    {
                        RaiseStepState(_index, _phase, StepResult.Succeeded, null);
                    }
                }
                else
                {
                    _logger.Info($"[Run] {step.DisplayName}");
                    step.Execute(_ctx);
                    _logger.Info($"[OK ] {step.DisplayName}");
                    RaiseStepState(_index, _phase, StepResult.Succeeded, null);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[FAIL] {step.DisplayName} :: {ex}");
                RaiseStepState(_index, _phase, StepResult.Failed, ex.Message);
                _hasAnyFailure = true;
            }
            finally
            {
                ReportProgress();
            }
        }

        /// <summary>
        /// Progress API 및 UI 표시용 진행률/설명 값을 갱신합니다.
        /// </summary>
        private void ReportProgress()
        {
            int totalSteps = _steps.Length;
            if (totalSteps <= 0) totalSteps = 1;

            // Validate + Execute(선택) 두 번 도는 구조이므로, 전체 진행률을 2-phase로 환산
            float phaseCount = _validateOnly ? 1f : 2f;
            float phaseIndex = _phase == StepPhase.Validate ? 0f : 1f;

            float local = Mathf.Clamp01((float)(_index + 1) / totalSteps);
            OverallProgress01 = Mathf.Clamp01((phaseIndex + local) / phaseCount);

            string msg = $"{PhaseDisplay}: {CurrentStepDisplay}";
            Progress.Report(_progressId, OverallProgress01, msg);
            Description = msg;
        }

        /// <summary>
        /// 스텝 상태 변경 이벤트를 발행합니다.
        /// </summary>
        /// <param name="index">스텝 인덱스(0 기반)입니다.</param>
        /// <param name="phase">현재 단계(Validate/Execute)입니다.</param>
        /// <param name="result">스텝 결과입니다.</param>
        /// <param name="message">보조 메시지입니다.</param>
        private void RaiseStepState(int index, StepPhase phase, StepResult result, string message)
        {
            OnStepStateChanged?.Invoke(index, phase, result, message);
        }

        /// <summary>
        /// 로거에서 추가된 로그 라인을 외부 구독자에게 전달합니다.
        /// </summary>
        /// <param name="line">추가된 로그 라인입니다.</param>
        private void HandleLogLine(string line)
        {
            OnLogLine?.Invoke(line);
        }

        /// <summary>
        /// 실행을 종료하고 Progress/틱/이벤트를 정리한 뒤 완료 이벤트를 발행합니다.
        /// </summary>
        /// <param name="state">종료 상태입니다.</param>
        private void Finish(RunState state)
        {
            if (!IsRunning) return;

            State = state;

            try
            {
                if (_progressId >= 0)
                    Progress.Remove(_progressId);
            }
            catch
            {
                // Progress 정리 실패는 치명적이지 않으므로 무시합니다.
            }

            EditorApplication.update -= Tick;

            try
            {
                _logger?.Info($"[Done] State: {State} / Log: {LogPath}");
            }
            catch
            {
                // 로깅 실패는 무시합니다.
            }

            try
            {
                OnCompleted?.Invoke(this);
            }
            catch
            {
                // 외부 구독자 예외로 러너가 크래시 나지 않도록 무시합니다.
            }
        }

        /// <summary>
        /// 러너가 사용한 리소스(Progress/틱/로거)를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            if (IsRunning)
            {
                EditorApplication.update -= Tick;
            }

            try
            {
                if (_progressId >= 0)
                    Progress.Remove(_progressId);
            }
            catch
            {
                // ignore
            }

            if (_logger != null)
            {
                _logger.OnLineAppended -= HandleLogLine;
                _logger.Dispose();
                _logger = null;
            }

            _ctx = null;
        }
    }
}
#endif
