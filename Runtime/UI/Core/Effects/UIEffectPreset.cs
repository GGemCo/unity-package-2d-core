using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 프리셋입니다.
    /// 현재는 컴포넌트/코드에서 기본 프리셋으로 사용하며,
    /// 이후 ScriptableObject 자산으로 확장할 수 있도록 구조를 분리합니다.
    /// </summary>
    [Serializable]
    public sealed class UIEffectPreset
    {
        public bool useUnscaledTime = true;

        public bool useFade;
        public float fadeDuration = 0.2f;
        public UiFadeUtility.FadeOptions fadeOptions = new UiFadeUtility.FadeOptions
        {
            delay = 0f,
            useUnscaledTime = true,
            updateInteractableOnComplete = true,
            updateBlocksRaycastsOnComplete = true,
            disableInputWhenInvisible = true,
            startAlpha = null,
            easeType = Easing.EaseType.EaseOutQuintic
        };

        public bool useMove;
        public Vector2 moveFromOffset;
        public float moveDuration = 0.22f;
        public MoveOptions moveOptions = new MoveOptions
        {
            delay = 0f,
            useUnscaledTime = true,
            easeType = Easing.EaseType.EaseOutQuintic,
            snapToTargetOnComplete = true
        };

        public bool useScalePulse;
        public Vector3 pulseScale = new Vector3(1.08f, 1.08f, 1f);
        public float scaleDuration = 0.16f;
        public Easing.EaseType scaleEaseType = Easing.EaseType.EaseOutBack;

        public bool useShake;
        public float shakeDuration = 0.16f;
        public float shakeStrength = 10f;
        public int shakeVibrato = 10;
        public Easing.EaseType shakeEaseType = Easing.EaseType.EaseOutQuad;

        public static UIEffectPreset CreateWindowOpenDefault()
        {
            return new UIEffectPreset
            {
                useUnscaledTime = true,
                useFade = true,
                fadeDuration = 0.24f,
                fadeOptions = new UiFadeUtility.FadeOptions
                {
                    delay = 0f,
                    useUnscaledTime = true,
                    updateInteractableOnComplete = true,
                    updateBlocksRaycastsOnComplete = true,
                    disableInputWhenInvisible = true,
                    startAlpha = 0f,
                    easeType = Easing.EaseType.EaseOutQuintic
                },
                useMove = true,
                moveFromOffset = new Vector2(0f, -18f),
                moveDuration = 0.24f,
                moveOptions = new MoveOptions
                {
                    delay = 0f,
                    useUnscaledTime = true,
                    easeType = Easing.EaseType.EaseOutQuintic,
                    snapToTargetOnComplete = true
                }
            };
        }

        public static UIEffectPreset CreateWindowCloseDefault()
        {
            return new UIEffectPreset
            {
                useUnscaledTime = true,
                useFade = true,
                fadeDuration = 0.18f,
                fadeOptions = new UiFadeUtility.FadeOptions
                {
                    delay = 0f,
                    useUnscaledTime = true,
                    updateInteractableOnComplete = true,
                    updateBlocksRaycastsOnComplete = true,
                    disableInputWhenInvisible = true,
                    startAlpha = null,
                    easeType = Easing.EaseType.EaseInQuintic
                }
            };
        }

        public static UIEffectPreset CreateResourceDecreaseDefault()
        {
            return new UIEffectPreset
            {
                useUnscaledTime = true,
                useShake = true,
                shakeDuration = 0.14f,
                shakeStrength = 10f,
                shakeVibrato = 10,
                shakeEaseType = Easing.EaseType.EaseOutQuad,
                useScalePulse = true,
                pulseScale = new Vector3(1.04f, 1.04f, 1f),
                scaleDuration = 0.14f,
                scaleEaseType = Easing.EaseType.EaseOutBack
            };
        }

        public static UIEffectPreset CreateResourceIncreaseDefault()
        {
            return new UIEffectPreset
            {
                useUnscaledTime = true,
                useScalePulse = true,
                pulseScale = new Vector3(1.06f, 1.06f, 1f),
                scaleDuration = 0.18f,
                scaleEaseType = Easing.EaseType.EaseOutBack
            };
        }

        public static UIEffectPreset CreateCooldownCompletedDefault()
        {
            return new UIEffectPreset
            {
                useUnscaledTime = true,
                useScalePulse = true,
                pulseScale = new Vector3(1.12f, 1.12f, 1f),
                scaleDuration = 0.18f,
                scaleEaseType = Easing.EaseType.EaseOutBack
            };
        }
    }
}
