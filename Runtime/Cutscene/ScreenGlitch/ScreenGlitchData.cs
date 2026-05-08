using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신에서 화면 글리치 효과를 재생하기 위한 데이터입니다.
    /// 전체 강도는 시간에 따라 보간되고, 세부 효과 값은 강도에 곱해져 적용됩니다.
    /// </summary>
    [Serializable]
    public class ScreenGlitchData
    {
        [Header("Strength")]
        [Tooltip("글리치 시작 강도입니다.")]
        [Range(0f, 1f)] public float fromIntensity = 0f;
        [Tooltip("글리치 종료 강도입니다.")]
        [Range(0f, 1f)] public float toIntensity = 1f;
        [Tooltip("클립 종료 후 마지막 글리치 상태를 유지할지 여부입니다.")]
        public bool holdFinalState;
        [Tooltip("컷신 종료 시 글리치 효과를 강제로 해제할지 여부입니다.")]
        public bool restoreOnCutsceneEnd = true;

        [Header("Distortion")]
        [Tooltip("RGB 채널 분리 강도입니다.")]
        [Range(0f, 0.05f)] public float rgbSplit = 0.012f;
        [Tooltip("가로 방향 줄 단위 흔들림 강도입니다.")]
        [Range(0f, 0.2f)] public float horizontalJitter = 0.045f;
        [Tooltip("세로 방향 순간 튐 강도입니다.")]
        [Range(0f, 0.1f)] public float verticalJump = 0.015f;
        [Tooltip("블록 단위 노이즈가 화면을 밀어내는 강도입니다.")]
        [Range(0f, 1f)] public float blockNoise = 0.35f;
        [Tooltip("스캔라인 어둡기 강도입니다.")]
        [Range(0f, 1f)] public float scanlineStrength = 0.25f;
        [Tooltip("색상 흔들림 강도입니다.")]
        [Range(0f, 1f)] public float colorDrift = 0.18f;

        [Header("Timing")]
        [Tooltip("Time.timeScale과 무관하게 진행할지 여부입니다.")]
        public bool useUnscaledTime = true;
        [Tooltip("글리치 노이즈 변화 속도입니다.")]
        [Min(0f)] public float noiseSpeed = 20f;
        [Tooltip("동일 설정에서도 패턴을 다르게 만들기 위한 시드입니다.")]
        public float seed = 0f;
        [Tooltip("강도 보간 easing 입니다.")]
        public Easing.EaseType easing = Easing.EaseType.Linear;
    }
}
