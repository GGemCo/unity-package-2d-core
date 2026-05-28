using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Crowd Control 적용 1회에 한해 시작 애니메이션 재생 방식을 덮어쓰기 위한 요청 데이터입니다.
    /// </summary>
    /// <remarks>
    /// 테이블 원본을 수정하지 않고 가드 브레이크, 패링 실패, 슈퍼아머 붕괴처럼
    /// 특정 피격 결과에서만 다른 초기 애니메이션과 재생 시간 정책을 적용할 때 사용합니다.
    /// </remarks>
    public struct CrowdControlAnimationOverride
    {
        /// <summary>초기 애니메이션 오버라이드를 사용할지 여부입니다.</summary>
        public bool UseInitialAnimationOverride;

        /// <summary>CC 시작 시 재생할 애니메이션 이름입니다.</summary>
        public string InitialAnimationName;

        /// <summary>루프 재생 여부입니다. 가드 브레이크 같은 단발 연출은 false를 사용합니다.</summary>
        public bool Loop;

        /// <summary>같은 애니메이션 상태여도 첫 프레임부터 다시 재생할지 여부입니다.</summary>
        public bool ForceReset;

        /// <summary>클립 길이가 목표 시간보다 긴 경우에만 TimeScale을 올려 목표 시간에 맞출지 여부입니다.</summary>
        public bool FitToTargetDurationWhenLonger;

        /// <summary>동기화 기준 시간(초)입니다. 일반적으로 Crowd Control Duration을 사용합니다.</summary>
        public float TargetDurationSeconds;

        /// <summary>목표 시간이 너무 짧을 때 사용할 최소 보정 시간(초)입니다.</summary>
        public float MinTargetDurationSeconds;

        /// <summary>과도한 빠른 재생을 막기 위한 최대 TimeScale입니다.</summary>
        public float MaxTimeScale;

        /// <summary>동적 TimeScale 보간으로 EaseType을 애니메이션 재생 속도에도 적용할지 여부입니다.</summary>
        public bool UseEasing;

        /// <summary>애니메이션 속도 보간에 사용할 Easing 타입입니다.</summary>
        public Easing.EaseType EaseType;

        /// <summary>KnockUp처럼 내부 Phase 애니메이션이 있는 CC에서, 오버라이드 애니메이션이 즉시 덮어써지지 않도록 막을지 여부입니다.</summary>
        public bool SuppressRuntimePhaseAnimations;

        /// <summary>초기 애니메이션 오버라이드가 유효한지 여부입니다.</summary>
        public bool IsValid => UseInitialAnimationOverride && !string.IsNullOrWhiteSpace(InitialAnimationName);

        /// <summary>
        /// 클립 길이와 목표 시간을 기준으로 실제 재생 TimeScale을 계산합니다.
        /// </summary>
        /// <param name="clipDurationSeconds">애니메이션 클립 길이(초)입니다.</param>
        /// <returns>적용할 TimeScale입니다.</returns>
        public float ResolveTimeScale(float clipDurationSeconds)
        {
            float timeScale = 1f;
            float clipDuration = Mathf.Max(0f, clipDurationSeconds);
            float minTargetDuration = Mathf.Max(0.0001f, MinTargetDurationSeconds);
            float targetDuration = Mathf.Max(minTargetDuration, TargetDurationSeconds);

            if (FitToTargetDurationWhenLonger)
            {
                if (clipDuration > targetDuration)
                    timeScale = clipDuration / targetDuration;
            }
            else if (targetDuration > 0f && clipDuration > 0f)
            {
                timeScale = clipDuration / targetDuration;
            }

            float maxTimeScale = MaxTimeScale > 0f ? MaxTimeScale : timeScale;
            return FitToTargetDurationWhenLonger
                ? Mathf.Clamp(timeScale, 1f, Mathf.Max(1f, maxTimeScale))
                : Mathf.Clamp(timeScale, 0.0001f, Mathf.Max(1f, maxTimeScale));
        }

        /// <summary>
        /// 실제 재생이 완료되는 시간을 계산합니다.
        /// </summary>
        /// <param name="clipDurationSeconds">애니메이션 클립 길이(초)입니다.</param>
        /// <returns>TimeScale 적용 후 예상 재생 시간(초)입니다.</returns>
        public float ResolvePlaybackDuration(float clipDurationSeconds)
        {
            float clipDuration = Mathf.Max(0f, clipDurationSeconds);
            if (clipDuration <= 0f)
                return 0f;

            float timeScale = ResolveTimeScale(clipDuration);
            if (timeScale <= 0f)
                return clipDuration;

            return clipDuration / timeScale;
        }
    }
}
