using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 타임라인에 맞춰 화면 글리치 효과의 강도와 세부 파라미터를 갱신하는 컨트롤러입니다.
    /// 실제 화면 렌더링은 <see cref="CutsceneGlitchService"/>와 RenderFeature가 수행합니다.
    /// </summary>
    public sealed class ScreenGlitchController : CutsceneDefaultController, ICutsceneController
    {
        private ScreenGlitchData _data;
        private float _elapsed;
        private float _duration;
        private bool _isPlaying;

        /// <summary>
        /// 화면 글리치 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public ScreenGlitchController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 별도 리소스 로드가 필요 없으므로 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;

        /// <summary>
        /// 화면 글리치 이벤트 실행 전 필요한 사전 준비를 수행합니다.
        /// 현재 구현은 런타임 서비스가 지연 생성되므로 별도 준비 작업이 없습니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.ScreenGlitch)
            {
                return;
            }
        }

        /// <summary>
        /// 화면 글리치 이벤트 실행 전 필요한 사전 준비를 코루틴 형태로 수행합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        /// <returns>준비 과정 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// 화면 글리치 이벤트를 시작하고 첫 프레임 상태를 렌더 서비스에 반영합니다.
        /// 지속 시간이 0 이하이면 종료 강도를 즉시 적용합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트 정보입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.ScreenGlitch)
            {
                return;
            }

            _data = evt.screenGlitch ?? new ScreenGlitchData();
            _duration = evt.duration > 0f ? evt.duration : 0f;
            _elapsed = 0f;
            _isPlaying = _duration > 0f;

            ApplyGlitch(_duration > 0f ? _data.fromIntensity : _data.toIntensity);
        }

        /// <summary>
        /// 시간 경과에 따라 글리치 강도를 보간하고, 클립 지속 시간이 끝나면 종료 처리를 수행합니다.
        /// </summary>
        public void Update()
        {
            if (!_isPlaying || _data == null)
            {
                return;
            }

            _elapsed += _data.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(t, _data.easing));
            float intensity = Mathf.Lerp(_data.fromIntensity, _data.toIntensity, eased);

            ApplyGlitch(intensity);

            if (_elapsed >= _duration)
            {
                Stop();
            }
        }

        /// <summary>
        /// 현재 글리치 진행을 중지하고 설정에 따라 마지막 상태 유지 또는 해제를 수행합니다.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;

            if (_data == null)
            {
                CutsceneGlitchService.ClearOwner(this);
                return;
            }

            if (_data.holdFinalState)
            {
                ApplyGlitch(_data.toIntensity);
                return;
            }

            CutsceneGlitchService.ClearOwner(this);
        }

        /// <summary>
        /// 컷신 종료 시 글리치 효과를 정리합니다.
        /// restoreOnCutsceneEnd가 꺼진 경우에는 마지막 상태를 유지할 수 있습니다.
        /// </summary>
        public void End()
        {
            _isPlaying = false;

            if (_data == null || _data.restoreOnCutsceneEnd)
            {
                CutsceneGlitchService.ClearOwner(this);
            }
        }

        /// <summary>
        /// 현재 강도와 데이터 설정을 합쳐 렌더 패스가 사용할 글리치 상태를 생성하고 서비스에 전달합니다.
        /// </summary>
        /// <param name="intensity">보간이 반영된 현재 글리치 전체 강도입니다.</param>
        private void ApplyGlitch(float intensity)
        {
            if (_data == null)
            {
                CutsceneGlitchService.ClearOwner(this);
                return;
            }

            intensity = Mathf.Clamp01(intensity);
            if (intensity <= 0.0001f)
            {
                CutsceneGlitchService.ClearOwner(this);
                return;
            }

            var state = new ScreenGlitchState
            {
                Intensity = intensity,
                RgbSplit = _data.rgbSplit,
                HorizontalJitter = _data.horizontalJitter,
                VerticalJump = _data.verticalJump,
                BlockNoise = _data.blockNoise,
                ScanlineStrength = _data.scanlineStrength,
                ColorDrift = _data.colorDrift,
                NoiseSpeed = _data.noiseSpeed,
                Seed = _data.seed,
            };

            CutsceneGlitchService.ApplyState(this, state);
        }
    }
}
