using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 카메라 흔들림이 어떤 방식으로 재생될지 결정하는 타입입니다.
    /// </summary>
    public enum CameraShakeType
    {
        /// <summary>
        /// 여러 방향으로 진동하며 시간이 지날수록 약해지는 일반 흔들림입니다.
        /// </summary>
        Common = 0,

        /// <summary>
        /// 지정된 방향으로 한 번 밀렸다가 복귀하는 충격형 흔들림입니다.
        /// </summary>
        DirectionalImpulse = 1,

        /// <summary>
        /// 지정된 방향 축을 기준으로 여러 번 진동하는 흔들림입니다.
        /// </summary>
        DirectionalOscillation = 2,
    }

    /// <summary>
    /// 카메라 흔들림 세기가 시간에 따라 감소하는 방식을 결정합니다.
    /// </summary>
    public enum CameraShakeDecayMode
    {
        /// <summary>
        /// 시간이 지날수록 선형으로 약해집니다.
        /// </summary>
        Linear = 0,

        /// <summary>
        /// 초반에는 강하고 후반으로 갈수록 부드럽게 약해집니다.
        /// </summary>
        Smooth = 1,
    }

    /// <summary>
    /// 카메라 흔들림 재생에 사용할 공통 프리셋입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraShakePreset", menuName = "GGemCo/Core/카메라 Shake Preset", order = 0)]
    public sealed class CameraShakePreset : ScriptableObject
    {
        [Header("설명")]
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Shake")]
        [Tooltip("카메라 흔들림 재생 방식입니다. 기본값은 일반적으로 사용하는 흔들림입니다.")]
        [SerializeField] private CameraShakeType shakeType = CameraShakeType.Common;
        [Tooltip("전체 흔들림 재생 시간(초)입니다.")]
        [SerializeField] private float duration = 0.2f;
        [Tooltip("흔들림 기본 세기입니다.")]
        [SerializeField] private float strength = 0.1f;
        [Tooltip("일반 흔들림에서 X/Y 축 세기 비율입니다. X=1, Y=0.5이면 좌우가 상하보다 강합니다.")]
        [SerializeField] private Vector2 axisStrength = new Vector2(1f, 0.5f);
        [Tooltip("진동형 흔들림에서 사용할 파형 반복 횟수입니다.")]
        [SerializeField] private int repeatCount = 3;
        [Tooltip("진동형 흔들림의 시작 위상을 매 재생마다 무작위로 정할지 여부입니다.")]
        [SerializeField] private bool randomStartPhase = true;
        [Tooltip("Time.timeScale 영향을 무시하고 흔들림을 재생할지 여부입니다.")]
        [SerializeField] private bool useUnscaledTime;

        [Header("Directional Impulse")]
        [Tooltip("DirectionalImpulse 타입에서 시간에 따른 세기 변화를 지정합니다. 비어 있으면 기본 복귀 곡선을 사용합니다.")]
        [SerializeField] private AnimationCurve impulseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Decay")]
        [Tooltip("진동형 흔들림이 시간에 따라 약해지는 방식입니다.")]
        [SerializeField] private CameraShakeDecayMode decayMode = CameraShakeDecayMode.Linear;

        public string Description => description;
        public CameraShakeType ShakeType => shakeType;
        public float Duration => duration;
        public float Strength => strength;
        public Vector2 AxisStrength => axisStrength;
        public int RepeatCount => repeatCount;
        public bool RandomStartPhase => randomStartPhase;
        public bool UseUnscaledTime => useUnscaledTime;
        public AnimationCurve ImpulseCurve => impulseCurve;
        public CameraShakeDecayMode DecayMode => decayMode;

        /// <summary>
        /// 프리셋에 저장된 흔들림 설정을 재생 요청으로 변환합니다.
        /// </summary>
        /// <param name="channel">흔들림을 분류하고 중단할 때 사용할 채널입니다.</param>
        /// <returns>카메라 매니저가 재생할 수 있는 흔들림 요청 데이터입니다.</returns>
        public CameraShakeRequest ToRequest(CameraShakeChannel channel = CameraShakeChannel.Default)
        {
            return CreateRequest(channel, Vector2.right, false);
        }

        /// <summary>
        /// 프리셋 설정과 외부에서 계산한 방향을 조합해 흔들림 요청으로 변환합니다.
        /// </summary>
        /// <param name="channel">흔들림을 분류하고 중단할 때 사용할 채널입니다.</param>
        /// <param name="direction">방향성 흔들림에 사용할 월드 방향입니다.</param>
        /// <returns>카메라 매니저가 재생할 수 있는 흔들림 요청 데이터입니다.</returns>
        public CameraShakeRequest ToDirectionalRequest(CameraShakeChannel channel, Vector2 direction)
        {
            return CreateRequest(channel, direction, true);
        }

        /// <summary>
        /// 공통 설정과 방향 설정을 정규화해 재생 요청을 생성합니다.
        /// </summary>
        /// <param name="channel">흔들림을 분류하고 중단할 때 사용할 채널입니다.</param>
        /// <param name="direction">방향성 흔들림에 사용할 월드 방향입니다.</param>
        /// <param name="forceDirectional">프리셋 타입이 일반 흔들림이어도 방향성 요청으로 강제할지 여부입니다.</param>
        /// <returns>정규화된 흔들림 요청 데이터입니다.</returns>
        private CameraShakeRequest CreateRequest(CameraShakeChannel channel, Vector2 direction, bool forceDirectional)
        {
            Vector2 safeAxisStrength = new Vector2(Mathf.Max(0f, axisStrength.x), Mathf.Max(0f, axisStrength.y));
            float safeStrength = Mathf.Max(0f, strength);
            CameraShakeType resolvedType = forceDirectional && shakeType == CameraShakeType.Common
                ? CameraShakeType.DirectionalImpulse
                : shakeType;

            return new CameraShakeRequest
            {
                ShakeType = resolvedType,
                Duration = Mathf.Max(0f, duration),
                Strength = safeStrength,
                AxisStrength = safeAxisStrength,
                Direction = direction,
                RepeatCount = Mathf.Max(1, repeatCount),
                RandomStartPhase = randomStartPhase,
                Channel = channel,
                UseUnscaledTime = useUnscaledTime,
                ImpulseCurve = impulseCurve,
                DecayMode = decayMode,
            };
        }
    }
}
