using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 공용 UI 효과 프리셋 데이터입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UIEffectPreset", menuName = "GGemCo/UI/UI Effect Preset")]
    public sealed class UIEffectPreset : ScriptableObject
    {
        [Header("Time")]
        [Tooltip("TimeScale 영향을 받지 않는 시간으로 효과를 재생할지 여부")]
        public bool useUnscaledTime = true;
        [Tooltip("효과 간 간섭을 제어할 채널")]
        public UIEffectChannel channel = UIEffectChannel.Default;
        [Tooltip("같은 대상에 동일 채널 효과가 재생 중일 때 처리 정책")]
        public UIEffectPlayPolicy playPolicy = UIEffectPlayPolicy.StopSameChannelAndPlay;

        [Header("Fade")]
        [Tooltip("Fade 효과 사용 여부")]
        public bool useFade;
        [Tooltip("Fade 시작 알파. 음수면 현재 값을 유지합니다.")]
        public float fadeStartAlpha = -1f;
        [Tooltip("Fade 목표 알파")]
        [Range(0f, 1f)] public float fadeTargetAlpha = 1f;
        [Tooltip("Fade 시간")]
        public float fadeDuration = 0.2f;
        [Tooltip("Fade 이징")]
        public Easing.EaseType fadeEaseType = Easing.EaseType.Linear;
        [Tooltip("Fade 완료 후 interactable 동기화 여부")]
        public bool fadeUpdateInteractableOnComplete = true;
        [Tooltip("Fade 완료 후 blocksRaycasts 동기화 여부")]
        public bool fadeUpdateBlocksRaycastsOnComplete = true;
        [Tooltip("알파가 0일 때 입력을 비활성화할지 여부")]
        public bool fadeDisableInputWhenInvisible = true;

        [Header("Move")]
        [Tooltip("AnchoredPosition 이동 효과 사용 여부")]
        public bool useMove;
        [Tooltip("기준 위치와 오프셋 적용 방향")]
        public UIEffectMoveMode moveMode = UIEffectMoveMode.FromOffsetToBase;
        [Tooltip("기준 위치 대비 적용할 오프셋")]
        public Vector2 moveFromOffset = Vector2.zero;
        [Tooltip("이동 시간")]
        public float moveDuration = 0.2f;
        [Tooltip("이동 이징")]
        public Easing.EaseType moveEaseType = Easing.EaseType.EaseOutCubic;
        [Tooltip("이동 종료 시 목표 위치로 스냅할지 여부")]
        public bool moveSnapToTargetOnComplete = true;

        [Header("Scale")]
        [Tooltip("절대 스케일 애니메이션 사용 여부")]
        public bool useScale;
        [Tooltip("시작 스케일")]
        public Vector3 scaleFrom = Vector3.one;
        [Tooltip("종료 스케일")]
        public Vector3 scaleTo = Vector3.one;
        [Tooltip("스케일 애니메이션 시간")]
        public float scaleDuration = 0.15f;
        [Tooltip("스케일 애니메이션 이징")]
        public Easing.EaseType scaleEaseType = Easing.EaseType.EaseOutCubic;

        [Header("Punch")]
        [Tooltip("펀치 스케일 효과 사용 여부")]
        public bool usePunchScale;
        [Tooltip("기준 스케일 대비 추가로 줄 펀치 값")]
        public Vector3 punchScale = new Vector3(0.08f, 0.08f, 0f);
        [Tooltip("펀치 시간")]
        public float punchDuration = 0.15f;
        [Tooltip("펀치 이징")]
        public Easing.EaseType punchEaseType = Easing.EaseType.EaseOutBack;

        [Header("Shake")]
        [Tooltip("흔들기 효과 사용 여부")]
        public bool useShake;
        [Tooltip("흔들기 강도")]
        public float shakeStrength = 8f;
        [Tooltip("흔들기 시간")]
        public float shakeDuration = 0.15f;
        [Tooltip("진동 횟수")]
        public int shakeVibrato = 14;

        [Header("Flash")]
        [Tooltip("Graphic 색상 플래시 효과 사용 여부")]
        public bool useFlash;
        [Tooltip("플래시에 사용할 색상")]
        public Color flashColor = Color.white;
        [Tooltip("플래시 색상의 최대 알파")]
        [Range(0f, 1f)] public float flashPeakAlpha = 0.8f;
        [Tooltip("플래시 전체 시간")]
        public float flashDuration = 0.2f;
        [Tooltip("플래시 이징")]
        public Easing.EaseType flashEaseType = Easing.EaseType.EaseOutCubic;
    }
}
