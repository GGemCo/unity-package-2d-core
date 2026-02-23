using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 수평(또는 평면) 이동 후 Hold(정지)를 수행하는 Solver입니다.
    /// - KnockDown(DownWaitTime)과 같은 케이스를 위해 분리합니다.
    /// </summary>
    internal sealed class MotionSolverLinearMoveHold : IMotionSolver
    {
        public static readonly MotionSolverLinearMoveHold Instance = new MotionSolverLinearMoveHold();
        private MotionSolverLinearMoveHold() { }

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
                // 이동은 끝났고, 이제 Hold만 남은 상태
                state.HoldRemaining -= dt;
                if (state.HoldRemaining <= 0f)
                    state.MarkComplete();
            }
        }
    }
}
