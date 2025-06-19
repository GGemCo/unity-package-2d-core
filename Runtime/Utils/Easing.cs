namespace GGemCo.Scripts
{
    public static class Easing
    {
        public enum EaseType
        {
            Linear,
            EaseInQuintic,
            EaseOutQuintic,
            EaseInQuad,
            EaseOutQuad,
            EaseInOutQuad
        }
        public static float EaseInQuad(float t) => t * t;
        public static float EaseOutQuad(float t) => t * (2 - t);
        public static float EaseInOutQuad(float t) => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;

        public static float EaseInQuintic(float t) => t * t * t * t * t;
        public static float EaseOutQuintic(float t) => 1 + (--t) * t * t * t * t;
        public static float EaseInOutQuintic(float t) => t < 0.5f ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t;
        
        public static float Apply(float t, EaseType type)
        {
            switch (type)
            {
                case EaseType.EaseInQuad: return EaseInQuad(t);
                case EaseType.EaseOutQuad: return EaseOutQuad(t);
                case EaseType.EaseInOutQuad: return EaseInOutQuad(t);
                case EaseType.EaseInQuintic: return EaseInQuintic(t);
                case EaseType.EaseOutQuintic: return EaseOutQuintic(t);
                case EaseType.Linear:
                default: return t;
            }
        }
    }
}
