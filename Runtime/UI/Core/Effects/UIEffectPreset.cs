using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 프리셋 데이터입니다.
    /// ScriptableObject 에셋으로 생성하여 윈도우/리소스 UI에서 공통으로 재사용할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UIEffectPreset", menuName = "GGemCo/UI/UI Effect Preset")]
    public sealed class UIEffectPreset : ScriptableObject
    {
        [Header("Common")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Fade")]
        [SerializeField] private bool useFade;
        [SerializeField] private float fadeDuration = 0.2f;
        [SerializeField] private UiFadeUtility.FadeOptions fadeOptions = new UiFadeUtility.FadeOptions
        {
            delay = 0f,
            useUnscaledTime = true,
            updateInteractableOnComplete = true,
            updateBlocksRaycastsOnComplete = true,
            disableInputWhenInvisible = true,
            startAlpha = null,
            easeType = Easing.EaseType.EaseOutQuintic
        };

        [Header("Move")]
        [SerializeField] private bool useMove;
        [SerializeField] private Vector2 moveFromOffset;
        [SerializeField] private float moveDuration = 0.22f;
        [SerializeField] private MoveOptions moveOptions = new MoveOptions
        {
            delay = 0f,
            useUnscaledTime = true,
            easeType = Easing.EaseType.EaseOutQuintic,
            snapToTargetOnComplete = true
        };

        [Header("Scale Pulse")]
        [SerializeField] private bool useScalePulse;
        [SerializeField] private Vector3 pulseScale = new(1.08f, 1.08f, 1f);
        [SerializeField] private float scaleDuration = 0.16f;
        [SerializeField] private Easing.EaseType scaleEaseType = Easing.EaseType.EaseOutBack;

        [Header("Shake")]
        [SerializeField] private bool useShake;
        [SerializeField] private float shakeDuration = 0.16f;
        [SerializeField] private float shakeStrength = 10f;
        [SerializeField] private int shakeVibrato = 10;
        [SerializeField] private Easing.EaseType shakeEaseType = Easing.EaseType.EaseOutQuad;

        public bool UseUnscaledTime => useUnscaledTime;
        public bool UseFade => useFade;
        public float FadeDuration => fadeDuration;
        public UiFadeUtility.FadeOptions FadeOptions => fadeOptions;
        public bool UseMove => useMove;
        public Vector2 MoveFromOffset => moveFromOffset;
        public float MoveDuration => moveDuration;
        public MoveOptions MoveOptions => moveOptions;
        public bool UseScalePulse => useScalePulse;
        public Vector3 PulseScale => pulseScale;
        public float ScaleDuration => scaleDuration;
        public Easing.EaseType ScaleEaseType => scaleEaseType;
        public bool UseShake => useShake;
        public float ShakeDuration => shakeDuration;
        public float ShakeStrength => shakeStrength;
        public int ShakeVibrato => shakeVibrato;
        public Easing.EaseType ShakeEaseType => shakeEaseType;

        private static UIEffectPreset _windowOpenFallback;
        private static UIEffectPreset _windowCloseFallback;
        private static UIEffectPreset _resourceIncreaseFallback;
        private static UIEffectPreset _resourceDecreaseFallback;
        private static UIEffectPreset _resourceMaxChangedFallback;
        private static UIEffectPreset _cooldownCompletedFallback;

        public static UIEffectPreset WindowOpenFallback => _windowOpenFallback != null ? _windowOpenFallback : _windowOpenFallback = CreateWindowOpenFallback();
        public static UIEffectPreset WindowCloseFallback => _windowCloseFallback != null ? _windowCloseFallback : _windowCloseFallback = CreateWindowCloseFallback();
        public static UIEffectPreset ResourceIncreaseFallback => _resourceIncreaseFallback != null ? _resourceIncreaseFallback : _resourceIncreaseFallback = CreateResourceIncreaseFallback();
        public static UIEffectPreset ResourceDecreaseFallback => _resourceDecreaseFallback != null ? _resourceDecreaseFallback : _resourceDecreaseFallback = CreateResourceDecreaseFallback();
        public static UIEffectPreset ResourceMaxChangedFallback => _resourceMaxChangedFallback != null ? _resourceMaxChangedFallback : _resourceMaxChangedFallback = CreateResourceMaxChangedFallback();
        public static UIEffectPreset CooldownCompletedFallback => _cooldownCompletedFallback != null ? _cooldownCompletedFallback : _cooldownCompletedFallback = CreateCooldownCompletedFallback();

        private static UIEffectPreset CreateWindowOpenFallback()
        {
            var preset = CreateRuntimeInstance("UIEffectPreset_WindowOpen_Default");
            preset.useUnscaledTime = true;
            preset.useFade = true;
            preset.fadeDuration = 0.24f;
            preset.fadeOptions = new UiFadeUtility.FadeOptions
            {
                delay = 0f,
                useUnscaledTime = true,
                updateInteractableOnComplete = true,
                updateBlocksRaycastsOnComplete = true,
                disableInputWhenInvisible = true,
                startAlpha = 0f,
                easeType = Easing.EaseType.EaseOutQuintic
            };
            preset.useMove = true;
            preset.moveFromOffset = new Vector2(0f, -18f);
            preset.moveDuration = 0.24f;
            preset.moveOptions = new MoveOptions
            {
                delay = 0f,
                useUnscaledTime = true,
                easeType = Easing.EaseType.EaseOutQuintic,
                snapToTargetOnComplete = true
            };
            return preset;
        }

        private static UIEffectPreset CreateWindowCloseFallback()
        {
            var preset = CreateRuntimeInstance("UIEffectPreset_WindowClose_Default");
            preset.useUnscaledTime = true;
            preset.useFade = true;
            preset.fadeDuration = 0.18f;
            preset.fadeOptions = new UiFadeUtility.FadeOptions
            {
                delay = 0f,
                useUnscaledTime = true,
                updateInteractableOnComplete = true,
                updateBlocksRaycastsOnComplete = true,
                disableInputWhenInvisible = true,
                startAlpha = null,
                easeType = Easing.EaseType.EaseInQuintic
            };
            preset.useMove = true;
            preset.moveFromOffset = new Vector2(0f, -12f);
            preset.moveDuration = 0.18f;
            preset.moveOptions = new MoveOptions
            {
                delay = 0f,
                useUnscaledTime = true,
                easeType = Easing.EaseType.EaseInQuad,
                snapToTargetOnComplete = true
            };
            return preset;
        }

        private static UIEffectPreset CreateResourceIncreaseFallback()
        {
            var preset = CreateRuntimeInstance("UIEffectPreset_HudIncrease_Default");
            preset.useUnscaledTime = true;
            preset.useScalePulse = true;
            preset.pulseScale = new Vector3(1.06f, 1.06f, 1f);
            preset.scaleDuration = 0.18f;
            preset.scaleEaseType = Easing.EaseType.EaseOutBack;
            return preset;
        }

        private static UIEffectPreset CreateResourceDecreaseFallback()
        {
            var preset = CreateRuntimeInstance("UIEffectPreset_HudDecrease_Default");
            preset.useUnscaledTime = true;
            preset.useShake = true;
            preset.shakeDuration = 0.14f;
            preset.shakeStrength = 10f;
            preset.shakeVibrato = 10;
            preset.shakeEaseType = Easing.EaseType.EaseOutQuad;
            preset.useScalePulse = true;
            preset.pulseScale = new Vector3(1.04f, 1.04f, 1f);
            preset.scaleDuration = 0.14f;
            preset.scaleEaseType = Easing.EaseType.EaseOutBack;
            return preset;
        }

        private static UIEffectPreset CreateResourceMaxChangedFallback()
        {
            var preset = CreateRuntimeInstance("UIEffectPreset_HudMaxChanged_Default");
            preset.useUnscaledTime = true;
            preset.useScalePulse = true;
            preset.pulseScale = new Vector3(1.05f, 1.05f, 1f);
            preset.scaleDuration = 0.16f;
            preset.scaleEaseType = Easing.EaseType.EaseOutBack;
            return preset;
        }

        private static UIEffectPreset CreateCooldownCompletedFallback()
        {
            var preset = CreateRuntimeInstance("UIEffectPreset_CooldownCompleted_Default");
            preset.useUnscaledTime = true;
            preset.useScalePulse = true;
            preset.pulseScale = new Vector3(1.12f, 1.12f, 1f);
            preset.scaleDuration = 0.18f;
            preset.scaleEaseType = Easing.EaseType.EaseOutBack;
            return preset;
        }

        private static UIEffectPreset CreateRuntimeInstance(string name)
        {
            var preset = CreateInstance<UIEffectPreset>();
            preset.name = name;
            preset.hideFlags = HideFlags.HideAndDontSave;
            return preset;
        }
    }
}
