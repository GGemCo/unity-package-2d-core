using UnityEngine;

namespace GGemCo2DCore
{
    internal abstract class CrowdControlMotionBase : ICrowdControlMotion
    {
        public bool IsFinished { get; protected set; }

        protected float Elapsed;
        protected float Duration;

        protected Vector2 StartPos;
        protected Vector2 EndPos;

        protected Easing.EaseType EaseType;

        protected CrowdControlMotionBase(Vector2 startPos, Vector2 endPos, float duration, Easing.EaseType easeType)
        {
            StartPos = startPos;
            EndPos = endPos;
            Duration = Mathf.Max(0f, duration);
            EaseType = easeType;
        }

        public abstract bool Tick(float deltaTime, out Vector2 nextPosition);

        protected float GetNormalizedTime()
        {
            if (Duration <= 0f) return 1f;
            return Mathf.Clamp01(Elapsed / Duration);
        }

        protected float GetEasedTime(float t)
        {
            return Mathf.Clamp01(Easing.Apply(t, EaseType));
        }
    }
}
