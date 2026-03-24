using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 현재 위치에서 목표 착지 지점까지 내려치기 형태로 이동시키는 Solver 입니다.
    /// </summary>
    internal sealed class MotionSolverGroundSlam : IMotionSolver
    {
        public static readonly MotionSolverGroundSlam Instance = new MotionSolverGroundSlam();
        private MotionSolverGroundSlam() { }

        public void Tick(ref CharacterMotionController2D.MotionState state, float dt, out Vector2 delta)
        {
            state.Elapsed += dt;

            float t = state.Duration <= 1e-6f ? 1f : Mathf.Clamp01(state.Elapsed / state.Duration);
            float eased = Easing.Apply(t, state.EaseType);

            Vector2 current = state.CurrentPosition;
            Vector2 desired = Vector2.LerpUnclamped(state.StartPosition, state.TargetPosition, eased);
            delta = desired - current;
            state.CurrentPosition = desired;

            bool reachedGround = desired.y <= state.TargetPosition.y + state.GroundSnapDistance;
            if (reachedGround)
            {
                Vector2 snapped = new Vector2(desired.x, state.TargetPosition.y);
                delta = snapped - current;
                state.CurrentPosition = snapped;
                state.MarkComplete();
                return;
            }

            if (t >= 1f)
            {
                delta = state.TargetPosition - current;
                state.CurrentPosition = state.TargetPosition;
                state.MarkComplete();
            }
        }
    }
}
