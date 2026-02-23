using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 수평(또는 평면) Distance 기반 이동 Solver입니다.
    /// </summary>
    internal sealed class MotionSolverLinearMove : IMotionSolver
    {
        public static readonly MotionSolverLinearMove Instance = new MotionSolverLinearMove();
        private MotionSolverLinearMove() { }

        public void Tick(ref CharacterMotionController2D.MotionState state, float dt, out Vector2 delta)
        {
            state.Elapsed += dt;

            float t = state.Duration <= 1e-6f ? 1f : Mathf.Clamp01(state.Elapsed / state.Duration);
            float eased = Easing.Apply(t, state.EaseType);

            float targetDistance = state.Distance * eased;
            float deltaDistance = targetDistance - state.MovedDistance;
            state.MovedDistance = targetDistance;

            delta = state.Direction * deltaDistance;

            if (t >= 1f)
            {
                // Linear 단독은 이동 완료 즉시 종료(홀드는 별도 Solver가 담당)
                state.MarkComplete();
            }
        }
    }
}
