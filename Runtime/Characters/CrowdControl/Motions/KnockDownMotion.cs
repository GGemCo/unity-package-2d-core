using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Knockdown: (선택) 짧은 이동 후, DownWaitTime 동안 위치 고정(눕기 유지)합니다.
    /// - 이동 구간: Duration 동안 StartPos -> EndPos (Easing)
    /// - 대기 구간: DownWaitTime 동안 EndPos 유지
    /// </summary>
    internal sealed class KnockDownMotion : CrowdControlMotionBase
    {
        private readonly float _downWaitTime;

        public KnockDownMotion(Vector2 startPos, Vector2 endPos, float moveDuration, Easing.EaseType easeType, float downWaitTime)
            : base(startPos, endPos, moveDuration, easeType)
        {
            _downWaitTime = Mathf.Max(0f, downWaitTime);
        }

        public override bool Tick(float deltaTime, out Vector2 nextPosition)
        {
            if (IsFinished)
            {
                nextPosition = EndPos;
                return false;
            }

            // 1) 이동 구간
            if (Duration > 0f && Elapsed < Duration)
            {
                Elapsed += deltaTime;

                if (Elapsed >= Duration)
                {
                    // 이동 종료 - EndPos로 스냅
                    nextPosition = EndPos;
                    // 이후는 대기 구간이 있으면 계속 진행
                    // 남은 delta는 단순히 다음 틱에서 처리 (정확한 분배는 큰 의미 없음)
                }
                else
                {
                    float t = GetNormalizedTime();
                    float easedT = GetEasedTime(t);
                    nextPosition = Vector2.LerpUnclamped(StartPos, EndPos, easedT);
                    return true;
                }
            }

            // 2) 대기 구간
            if (_downWaitTime > 0f)
            {
                // moveDuration 종료 이후부터 대기 타이머를 별도로 재기 위해 Elapsed를 재사용하지 않고,
                // 간단히 (Elapsed - Duration)로 계산합니다.
                float downElapsed = Mathf.Max(0f, Elapsed - Duration);
                downElapsed += deltaTime;
                Elapsed = Duration + downElapsed;

                if (downElapsed >= _downWaitTime)
                {
                    IsFinished = true;
                    nextPosition = EndPos;
                    return true;
                }

                nextPosition = EndPos;
                return true;
            }

            // 3) 대기 시간이 없으면 즉시 종료
            IsFinished = true;
            nextPosition = EndPos;
            return true;
        }
    }
}
