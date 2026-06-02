using UnityEngine;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = "CameraShakePreset", menuName = "GGemCo/Core/카메라 Shake Preset", order = 0)]
    public sealed class CameraShakePreset : ScriptableObject
    {
        [Header("설명")]
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Shake")]
        [Tooltip("전체 Shake 재생 시간(초)")]
        [SerializeField] private float duration = 0.2f;
        [Tooltip("좌우/상하 파형 반복 횟수")]
        [SerializeField] private int repeatCount = 3;
        [Tooltip("왼쪽으로 흔들릴 때 최대 세기")]
        [SerializeField] private float leftStrength = 0.1f;
        [Tooltip("오른쪽으로 흔들릴 때 최대 세기")]
        [SerializeField] private float rightStrength = 0.1f;
        [Tooltip("아래쪽으로 흔들릴 때 최대 세기")]
        [SerializeField] private float downStrength = 0.05f;
        [Tooltip("위쪽으로 흔들릴 때 최대 세기")]
        [SerializeField] private float upStrength = 0.05f;
        [Tooltip("Time.timeScale 영향을 무시하고 흔들림을 재생할지 여부")]
        [SerializeField] private bool useUnscaledTime;

        [Header("Start Phase")]
        [Tooltip("Shake 파형 시작 위치를 매번 랜덤으로 정할지, 고정 각도로 시작할지 결정합니다.")]
        [SerializeField] private CameraShakePhaseMode phaseMode = CameraShakePhaseMode.Random;
        [Tooltip("고정 위상 모드에서 사용할 시작 각도입니다. 아래 방향 최대값은 90도 근처입니다.")]
        [SerializeField] private float fixedPhaseDegrees = 0f;

        public string Description => description;
        public float Duration => duration;
        public int RepeatCount => repeatCount;
        public float LeftStrength => leftStrength;
        public float RightStrength => rightStrength;
        public float DownStrength => downStrength;
        public float UpStrength => upStrength;
        public bool UseUnscaledTime => useUnscaledTime;
        public CameraShakePhaseMode PhaseMode => phaseMode;
        public float FixedPhaseDegrees => fixedPhaseDegrees;
        public float FixedPhaseRadians => fixedPhaseDegrees * Mathf.Deg2Rad;

        /// <summary>
        /// 프리셋에 저장된 Shake 설정을 런타임 재생 요청으로 변환합니다.
        /// </summary>
        /// <param name="channel">Shake를 식별하고 중단할 때 사용할 채널입니다.</param>
        /// <returns>카메라 매니저가 재생할 수 있는 Shake 요청 데이터입니다.</returns>
        public CameraShakeRequest ToRequest(CameraShakeChannel channel = CameraShakeChannel.Default)
        {
            return new CameraShakeRequest
            {
                Duration = Mathf.Max(0f, duration),
                RepeatCount = Mathf.Max(1, repeatCount),
                LeftStrength = Mathf.Max(0f, leftStrength),
                RightStrength = Mathf.Max(0f, rightStrength),
                DownStrength = Mathf.Max(0f, downStrength),
                UpStrength = Mathf.Max(0f, upStrength),
                Channel = channel,
                UseUnscaledTime = useUnscaledTime,
                PhaseMode = phaseMode,
                FixedPhaseRadians = FixedPhaseRadians,
            };
        }
    }
}
