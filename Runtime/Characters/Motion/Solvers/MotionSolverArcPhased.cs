using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Arc(상승/정점/낙하) 모션을 Distance/Height 진행률 기반으로 처리하는 Solver입니다.
    /// </summary>
    /// <remarks>
    /// - 진행축은 기본적으로 수평 누적 거리(0..Distance)를 사용합니다.
    /// - Distance가 0에 가깝다면 시간 기반 진행률(t)을 폴백으로 사용합니다(제자리 넉업 등).
    /// - Phase는 시간 대신 정규화 진행률(u) 구간으로 정의됩니다(예: Rise → ApexHold → Fall).
    /// </remarks>
    internal sealed class MotionSolverArcPhased : IMotionSolver
    {
        public static readonly MotionSolverArcPhased Instance = new MotionSolverArcPhased();
        private MotionSolverArcPhased() { }

        public void Tick(ref CharacterMotionController2D.MotionState state, float dt, out Vector2 delta)
        {
            state.Elapsed += dt;

            // 1) 수평(기본) 이동: Distance + Easing (기존과 동일)
            float t = state.Duration <= 1e-6f ? 1f : Mathf.Clamp01(state.Elapsed / state.Duration);
            float eased = Easing.Apply(t, state.EaseType);

            float targetDistance = state.Distance * eased;
            float deltaDistance = targetDistance - state.MovedDistance;
            state.MovedDistance = targetDistance;

            delta = state.Direction * deltaDistance;

            // 2) 진행률 u 결정
            float u;
            if (state.Distance > 1e-6f)
            {
                u = Mathf.Clamp01(state.MovedDistance / state.Distance);
            }
            else
            {
                // 제자리 Arc: 시간 기반 진행률 폴백
                u = t;
            }

            // 3) 수직 목표 y(u) 계산 (Rise → ApexHold → Fall)
            float height = state.ArcHeight;
            if (height > 0f)
            {
                float apexHoldWidth = Mathf.Clamp01(state.ArcApexHoldNormalized);
                float half = apexHoldWidth * 0.5f;
                float apexStart = Mathf.Clamp01(0.5f - half);
                float apexEnd = Mathf.Clamp01(0.5f + half);

                // 극단값 방지(구간 길이 0 방지)
                const float minGap = 0.02f;
                apexStart = Mathf.Clamp(apexStart, 0f + minGap, 1f - minGap);
                apexEnd = Mathf.Clamp(apexEnd, apexStart + minGap, 1f);

                float y;
                if (u < apexStart)
                {
                    // Rise: 0 -> H
                    float v = apexStart <= 1e-6f ? 1f : Mathf.Clamp01(u / apexStart);
                    float ve = Easing.Apply(v, state.ArcRiseEaseType);
                    y = height * ve;
                }
                else if (u <= apexEnd)
                {
                    // Apex hold
                    y = height;
                }
                else
                {
                    // Fall: H -> 0
                    float denom = Mathf.Max(1e-6f, 1f - apexEnd);
                    float v = Mathf.Clamp01((u - apexEnd) / denom);
                    float ve = Easing.Apply(v, state.ArcFallEaseType);
                    y = height * (1f - ve);
                }

                float deltaY = y - state.AppliedArcY;
                state.AppliedArcY = y;
                delta += Vector2.up * deltaY;
            }

            // 4) 완료/홀드 처리
            // - 진행률이 1에 도달했거나(거리/시간), t==1이면 이동 구간 종료로 간주합니다.
            bool mainComplete = (state.Distance > 1e-6f) ? (u >= 1f) : (t >= 1f);
            if (mainComplete)
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
