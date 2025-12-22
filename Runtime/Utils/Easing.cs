using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 수식 기반 Easing 유틸리티.
    /// Tween / Animation / UI 연출에서 공통 사용합니다.
    /// </summary>
    public static class Easing
    {
        public enum EaseType
        {
            Linear,

            // Quad
            EaseInQuad,
            EaseOutQuad,
            EaseInOutQuad,

            // Cubic
            EaseInCubic,
            EaseOutCubic,
            EaseInOutCubic,

            // Quart
            EaseInQuart,
            EaseOutQuart,
            EaseInOutQuart,

            // Quintic
            EaseInQuintic,
            EaseOutQuintic,
            EaseInOutQuintic,

            // Sine
            EaseInSine,
            EaseOutSine,
            EaseInOutSine,

            // Expo
            EaseInExpo,
            EaseOutExpo,
            EaseInOutExpo,

            // Circ
            EaseInCirc,
            EaseOutCirc,
            EaseInOutCirc,

            // Back
            EaseInBack,
            EaseOutBack,
            EaseInOutBack
        }

        // --------------------------------------------------------------------
        // Quad
        // --------------------------------------------------------------------
        public static float EaseInQuad(float t) => t * t;
        public static float EaseOutQuad(float t) => t * (2f - t);
        public static float EaseInOutQuad(float t) =>
            t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        // --------------------------------------------------------------------
        // Cubic
        // --------------------------------------------------------------------
        public static float EaseInCubic(float t) => t * t * t;
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float EaseInOutCubic(float t) =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        // --------------------------------------------------------------------
        // Quart
        // --------------------------------------------------------------------
        public static float EaseInQuart(float t) => t * t * t * t;
        public static float EaseOutQuart(float t) => 1f - Mathf.Pow(1f - t, 4f);
        public static float EaseInOutQuart(float t) =>
            t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f;

        // --------------------------------------------------------------------
        // Quintic
        // --------------------------------------------------------------------
        public static float EaseInQuintic(float t) => t * t * t * t * t;
        public static float EaseOutQuintic(float t) => 1f - Mathf.Pow(1f - t, 5f);
        public static float EaseInOutQuintic(float t) =>
            t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f;

        // --------------------------------------------------------------------
        // Sine
        // --------------------------------------------------------------------
        public static float EaseInSine(float t) =>
            1f - Mathf.Cos((t * Mathf.PI) * 0.5f);
        public static float EaseOutSine(float t) =>
            Mathf.Sin((t * Mathf.PI) * 0.5f);
        public static float EaseInOutSine(float t) =>
            -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;

        // --------------------------------------------------------------------
        // Expo
        // --------------------------------------------------------------------
        public static float EaseInExpo(float t) =>
            t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
        public static float EaseOutExpo(float t) =>
            t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
        public static float EaseInOutExpo(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return t < 0.5f
                ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f
                : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;
        }

        // --------------------------------------------------------------------
        // Circ
        // --------------------------------------------------------------------
        public static float EaseInCirc(float t) =>
            1f - Mathf.Sqrt(1f - t * t);
        public static float EaseOutCirc(float t) =>
            Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));
        public static float EaseInOutCirc(float t) =>
            t < 0.5f
                ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) * 0.5f
                : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) * 0.5f;

        // --------------------------------------------------------------------
        // Back (타격감 연출에 매우 유용)
        // --------------------------------------------------------------------
        private const float BackC1 = 1.70158f;
        private const float BackC2 = BackC1 * 1.525f;

        public static float EaseInBack(float t) =>
            (BackC1 + 1f) * t * t * t - BackC1 * t * t;

        public static float EaseOutBack(float t) =>
            1f + (BackC1 + 1f) * Mathf.Pow(t - 1f, 3f) + BackC1 * Mathf.Pow(t - 1f, 2f);

        public static float EaseInOutBack(float t) =>
            t < 0.5f
                ? (Mathf.Pow(2f * t, 2f) * ((BackC2 + 1f) * 2f * t - BackC2)) * 0.5f
                : (Mathf.Pow(2f * t - 2f, 2f) * ((BackC2 + 1f) * (t * 2f - 2f) + BackC2) + 2f) * 0.5f;

        // --------------------------------------------------------------------
        // Apply
        // --------------------------------------------------------------------
        public static float Apply(float t, EaseType type)
        {
            switch (type)
            {
                case EaseType.EaseInQuad:       return EaseInQuad(t);
                case EaseType.EaseOutQuad:      return EaseOutQuad(t);
                case EaseType.EaseInOutQuad:    return EaseInOutQuad(t);

                case EaseType.EaseInCubic:      return EaseInCubic(t);
                case EaseType.EaseOutCubic:     return EaseOutCubic(t);
                case EaseType.EaseInOutCubic:   return EaseInOutCubic(t);

                case EaseType.EaseInQuart:      return EaseInQuart(t);
                case EaseType.EaseOutQuart:     return EaseOutQuart(t);
                case EaseType.EaseInOutQuart:   return EaseInOutQuart(t);

                case EaseType.EaseInQuintic:    return EaseInQuintic(t);
                case EaseType.EaseOutQuintic:   return EaseOutQuintic(t);
                case EaseType.EaseInOutQuintic: return EaseInOutQuintic(t);

                case EaseType.EaseInSine:       return EaseInSine(t);
                case EaseType.EaseOutSine:      return EaseOutSine(t);
                case EaseType.EaseInOutSine:    return EaseInOutSine(t);

                case EaseType.EaseInExpo:       return EaseInExpo(t);
                case EaseType.EaseOutExpo:      return EaseOutExpo(t);
                case EaseType.EaseInOutExpo:    return EaseInOutExpo(t);

                case EaseType.EaseInCirc:       return EaseInCirc(t);
                case EaseType.EaseOutCirc:      return EaseOutCirc(t);
                case EaseType.EaseInOutCirc:    return EaseInOutCirc(t);

                case EaseType.EaseInBack:       return EaseInBack(t);
                case EaseType.EaseOutBack:      return EaseOutBack(t);
                case EaseType.EaseInOutBack:    return EaseInOutBack(t);

                case EaseType.Linear:
                default:
                    return t;
            }
        }
    }
}
