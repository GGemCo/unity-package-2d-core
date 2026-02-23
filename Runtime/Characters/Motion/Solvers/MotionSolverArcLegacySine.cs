using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 기존 Arc 구현(시간 t 기반 + sin(pi*t))을 유지하기 위한 Solver입니다.
    /// - 하위 호환 및 점진적 마이그레이션을 위해 별도 분리합니다.
    /// </summary>
    internal sealed class MotionSolverArcLegacySine : IMotionSolver
    {
        public static readonly MotionSolverArcLegacySine Instance = new MotionSolverArcLegacySine();

        private MotionSolverArcLegacySine() { }

        public void Tick(ref CharacterMotionController2D.MotionState state, float dt, out Vector2 delta)
        {
            state.Elapsed += dt;

            float t = state.Duration <= 1e-6f ? 1f : Mathf.Clamp01(state.Elapsed / state.Duration);
            float eased = Easing.Apply(t, state.EaseType);

            float targetDistance = state.Distance * eased;
            float deltaDistance = targetDistance - state.MovedDistance;
            state.MovedDistance = targetDistance;

            delta = state.Direction * deltaDistance;

            if (state.ArcHeight > 0f)
            {
                float arc = Mathf.Sin(Mathf.PI * t) * state.ArcHeight;
                float deltaArc = arc - state.AppliedArcY;
                state.AppliedArcY = arc;
                delta += Vector2.up * deltaArc;
            }

            // 완료/홀드 처리
            if (t >= 1f)
            {
                if (state.HoldSecondsAfter > 0f)
                {
                    state.HoldRemaining -= dt;
                    if (state.HoldRemaining <= 0f)
                        state.MarkComplete();
                }
                else
                {
                    state.MarkComplete();
                }
            }
        }
    }
}
