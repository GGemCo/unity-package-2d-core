using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// KnockUp: 수평 이동은 Easing, 수직 이동은 아크(arc)로 처리합니다.
    /// - 수직 아크는 sin(pi * t) * Height
    /// - Height가 0이면 수평 이동만 수행합니다.
    /// </summary>
    internal sealed class KnockUpMotion : CrowdControlMotionBase
    {
        private readonly float _height;

        public KnockUpMotion(Vector2 startPos, Vector2 endPos, float duration, Easing.EaseType easeType, float height)
            : base(startPos, endPos, duration, easeType)
        {
            _height = Mathf.Max(0f, height);
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

            float x = Mathf.LerpUnclamped(StartPos.x, EndPos.x, easedT);
            float baseY = Mathf.LerpUnclamped(StartPos.y, EndPos.y, easedT);

            // 아크(arc): 0 -> 최고점 -> 0
            float arcY = (_height <= 0f) ? 0f : Mathf.Sin(Mathf.PI * t) * _height;

            nextPosition = new Vector2(x, baseY + arcY);
            return true;
        }
    }
}
