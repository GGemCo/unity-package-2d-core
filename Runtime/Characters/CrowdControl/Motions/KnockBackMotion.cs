using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 기존 Knockback 방식: Duration 동안 총 Distance를 Easing으로 보간하여 이동합니다.
    /// </summary>
    internal sealed class KnockBackMotion : CrowdControlMotionBase
    {
        public KnockBackMotion(Vector2 startPos, Vector2 endPos, float duration, Easing.EaseType easeType)
            : base(startPos, endPos, duration, easeType)
        {
        }

        public override bool Tick(float deltaTime, out Vector2 nextPosition)
        {
            if (IsFinished)
            {
                nextPosition = EndPos;
                return false;
            }

            if (Duration <= 0f)
            {
                IsFinished = true;
                nextPosition = EndPos;
                return true;
            }

            Elapsed += deltaTime;

            if (Elapsed >= Duration)
            {
                IsFinished = true;
                nextPosition = EndPos;
                return true;
            }

            float t = GetNormalizedTime();
            float easedT = GetEasedTime(t);
            nextPosition = Vector2.LerpUnclamped(StartPos, EndPos, easedT);
            return true;
        }
    }
}
